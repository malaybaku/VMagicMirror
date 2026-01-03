using System;
using System.Collections.Generic;
using Mediapipe;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Tasks.Vision.FaceLandmarker;
using UnityEngine;
using R3;
using NormalizedLandmark = Mediapipe.Tasks.Components.Containers.NormalizedLandmark;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    // ref:
    // Holistic: https://ai.google.dev/edge/mediapipe/solutions/vision/holistic_landmarker?hl=ja
    // Hand: https://ai.google.dev/edge/mediapipe/solutions/vision/hand_landmarker?hl=ja
    // Pose: https://ai.google.dev/edge/mediapipe/solutions/vision/pose_landmarker?hl=ja
    // Face Detector: https://ai.google.dev/edge/mediapipe/solutions/vision/face_detector?hl=ja
    // Face Landmark: https://ai.google.dev/edge/mediapipe/solutions/vision/face_landmarker?hl=ja

    public abstract class MediaPipeTrackerTaskBase
    {
        public MediaPipeTrackerTaskBase(
            MediaPipeTrackerRuntimeSettingsRepository settingsRepository,
            WebCamTextureSource textureSource,
            MediaPipeKinematicSetter mediaPipeKinematicSetter,
            MediaPipeFacialValueRepository facialValueRepository,
            CameraCalibrator calibrator,
            LandmarksVisualizer landmarksVisualizer
        )
        {
            SettingsRepository = settingsRepository;
            _textureSource = textureSource;
            MediaPipeKinematicSetter = mediaPipeKinematicSetter;
            FacialValueRepository = facialValueRepository;
            Calibrator = calibrator;
            LandmarksVisualizer = landmarksVisualizer;
        }

        private readonly WebCamTextureSource _textureSource;

        private IDisposable _textureSourceSubscriber = null;

        protected MediaPipeKinematicSetter MediaPipeKinematicSetter { get; }
        
        protected MediaPipeFacialValueRepository FacialValueRepository { get; }

        // NOTE: visualizerはそのうち削除もアリ。Instantiateしないのが保証されてれば残ってもよいが
        protected LandmarksVisualizer LandmarksVisualizer { get; }

        protected MediaPipeTrackerRuntimeSettingsRepository SettingsRepository { get; }
        protected CameraCalibrator Calibrator { get; }

        protected int WebCamTextureWidth => _textureSource.Width;
        protected int WebCamTextureHeight => _textureSource.Height;

        /// <summary>
        /// NOTE: 横長になると1より大きくなる
        /// </summary>
        protected float WebCamTextureAspect => _textureSource.Width * 1f / _textureSource.Height;

        protected bool IsActive { get; private set; }
        
        protected abstract void OnStartTask();
        protected abstract void OnStopTask();
        protected abstract void OnWebCamImageUpdated(WebCamImageSource source);

        private int _onResultCalledCount;
        private long _onResultCalledPrevTimestamp;

        protected void LogOnResultCalled(long timestampMillisecond)
        {
            _onResultCalledCount++;
            // 割とテキトーに。
            if (_onResultCalledCount % 30 == 0)
            {
                var elapsedSeconds = (timestampMillisecond - _onResultCalledPrevTimestamp) * 0.001f;
                var calledRate = 30 / elapsedSeconds;
                Debug.Log($"OnResult: 30 times called, elapsed={elapsedSeconds:0.000}, fps={calledRate:0.0}");
                _onResultCalledPrevTimestamp = timestampMillisecond;
            }
        }

        public void SetTaskActive(bool isActive)
        {
            IsActive = isActive;
            if (isActive)
            {
                StartTask();
            }
            else
            {
                StopTask();
            }
        }

        protected void RestartTaskIfActive()
        {
            if (IsActive)
            {
                StartTask();
            }
        }
        
        private void StartTask()
        {
            StopTask();
            OnStartTask();

            // NOTE: OnResult的なやつが発火するまでIO<T>を無視するような実装もアリだが、
            // Mediapipeのdocによるとコールバックの発火側もよしなにdropすることがあるらしく、無視したらしたで面倒そうなので素通しする。
            // 負荷をケチる場合、そもそもtextureSource側でImageを生成するのをサボるとこまでやるのがよさそう
            _textureSourceSubscriber = _textureSource
                .ImageUpdated
                .Subscribe(OnWebCamImageUpdated);
        }

        public void StopTask()
        {
            OnStopTask();
            _textureSourceSubscriber?.Dispose();
            _textureSourceSubscriber = null;
        }

        // 手の位置が交差しており、それを問題視する(トラッキングロスト相当に扱いたい)場合はtrueを返す
        public bool IsCrossedWristPos(NormalizedLandmark wristLandmark, bool isLeft)
        {
            if (!SettingsRepository.GuardCrossingHand.Value)
            {
                return false;
            }
            
            // 手首の位置は normalized の画像座標に基づいた値を言う。このとき、縦横のスケールだけ合わせる
            var normalizedPos = MediapipeMathUtil.GetTrackingNormalizePosition(wristLandmark, WebCamTextureAspect);
            var posOffset = MediapipeMathUtil.GetNormalized2DofPositionDiff(normalizedPos, Calibrator.GetCalibrationData());

            // 画像座標が [-0.5, 0.5] の範囲なのを前提とした処理。しきい値はそのうち可変にしたくなるかもしれない
            return (isLeft, posOffset.x) switch 
            {
                (true, > 0.15f) => true,
                (false, < -0.15f) => true,
                _ => false,
            };
        }
        
        protected void SetLeftHandPose(NormalizedLandmarks landmarks, Landmarks worldLandmarks, MediaPipeFingerPoseCalculator fingerPoseCalculator)
        {
            // 指のFK + 手首のローカル回転の取得までは下記で実施
            fingerPoseCalculator.SetLeftHandPose(worldLandmarks);

            // 手首の位置は normalized の画像座標に基づいた値を言う。このとき、縦横のスケールだけ合わせる
            var wristLandmark = landmarks.landmarks[0];
            var normalizedPos = MediapipeMathUtil.GetTrackingNormalizePosition(wristLandmark, WebCamTextureAspect);
            var posOffset = MediapipeMathUtil.GetNormalized2DofPositionDiff(normalizedPos, Calibrator.GetCalibrationData());
            MediaPipeKinematicSetter.SetLeftHandPose(posOffset, fingerPoseCalculator.LeftHandRotation);
        }

        protected void SetRightHandPose(NormalizedLandmarks landmarks, Landmarks worldLandmarks, MediaPipeFingerPoseCalculator fingerPoseCalculator)
        {
            fingerPoseCalculator.SetRightHandPose(worldLandmarks);

            var wristLandmark = landmarks.landmarks[0];
            var normalizedPos = MediapipeMathUtil.GetTrackingNormalizePosition(wristLandmark, WebCamTextureAspect);
            var posOffset = MediapipeMathUtil.GetNormalized2DofPositionDiff(normalizedPos, Calibrator.GetCalibrationData());
            MediaPipeKinematicSetter.SetRightHandPose(posOffset, fingerPoseCalculator.RightHandRotation);
        }
    }

    // FaceLandmarkerの結果を受け取って処理を行うクラス。Faceを使うクラスがいくつかあるので、その共通部分だけ抜き出している
    public sealed class FaceLandmarkResultHandler
    {
        public FaceLandmarkResultHandler(
            WebCamTextureSource textureSource,
            MediaPipeTrackerRuntimeSettingsRepository settingsRepository,
            MediaPipeKinematicSetter mediaPipeKinematicSetter,
            MediaPipeFacialValueRepository facialValueRepository,
            CameraCalibrator calibrator,
            MediaPipeTrackerStatusPreviewSender previewSender
        )
        {
            _textureSource = textureSource;
            _settingsRepository = settingsRepository;
            _mediaPipeKinematicSetter = mediaPipeKinematicSetter;
            _facialValueRepository = facialValueRepository;
            _calibrator = calibrator;
            _previewSender = previewSender;
        }

        private readonly WebCamTextureSource _textureSource;
        private readonly MediaPipeKinematicSetter _mediaPipeKinematicSetter;
        private readonly MediaPipeFacialValueRepository _facialValueRepository;
        private readonly MediaPipeTrackerRuntimeSettingsRepository _settingsRepository;
        private readonly CameraCalibrator _calibrator;
        private readonly MediaPipeTrackerStatusPreviewSender _previewSender;
        
        // NOTE: 横長になると1より大きくなる
        private float WebCamTextureAspect => _textureSource.Width * 1f / _textureSource.Height;
        
        private readonly Dictionary<string, float> _blendShapeValues = new(52);

        public void OnFaceLandmarkResult(FaceLandmarkerResult result, bool expectBlendShapeOutput)
        {
            if (result.faceLandmarks is not { Count: > 0 } || 
                (expectBlendShapeOutput && result.faceBlendshapes is not { Count: > 0 }))
            {
                _facialValueRepository.RequestReset();
                _mediaPipeKinematicSetter.ClearHeadPose();
                return;
            }
            
            if (expectBlendShapeOutput && result.faceBlendshapes is { Count: > 0 })
            {
                // 一度入った BlendShape はPlayMode中に消えない…という前提を置いている
                foreach (var c in result.faceBlendshapes[0].categories)
                {
                    _blendShapeValues[c.categoryName] = c.score;
                }
                _facialValueRepository.SetValues(_blendShapeValues);

                var eye = _facialValueRepository.BlendShapes.Eye;
                _previewSender.SetBlinkResult(eye.LeftBlink, eye.RightBlink);
            }

            var matrix = result.facialTransformationMatrixes[0];
            var headPose = MediapipeMathUtil.GetCalibratedFaceLocalPose(matrix, _calibrator.GetCalibrationData());
            _mediaPipeKinematicSetter.SetHeadPose6Dof(headPose);

            if (_settingsRepository.HasCalibrationRequest)
            {
                _ = _calibrator.TrySetSixDofData(result, WebCamTextureAspect);
            }
        }
    }
}