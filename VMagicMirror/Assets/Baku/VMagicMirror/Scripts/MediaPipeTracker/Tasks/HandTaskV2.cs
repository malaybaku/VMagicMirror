using System;
using System.Linq;
using Mediapipe;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Tasks.Core;
using Mediapipe.Tasks.Vision.HolisticLandmarker;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    // NOTE: この単発クラスは「外部トラッキング(≒iFacialMocap) + ハンドトラッキング」の場合にだけ有効になり、
    // 「顔 + 手」の場合は代わりに HandAndFaceLandmarkTask のほうが動作する
    /// <summary>
    /// HolisticLandmarkerを使って1人分のハンドトラッキングを行うクラス
    /// </summary>
    public class HandTaskV2 : MediaPipeTrackerTaskBase, IHandLandmarkTask
    {
        private const string ModelFileName = "holistic_landmarker.bytes";
        
        protected MediaPipeTrackerStatusPreviewSender PreviewSender { get; }

        private readonly MediaPipeFingerPoseCalculator _fingerPoseCalculator;
        private HolisticLandmarker _landmarker;

        [Inject]
        public HandTaskV2(
            MediaPipeTrackerRuntimeSettingsRepository settingsRepository,
            WebCamTextureSource textureSource,
            MediaPipeKinematicSetter mediaPipeKinematicSetter, 
            MediaPipeFacialValueRepository facialValueRepository,
            CameraCalibrator calibrator,
            LandmarksVisualizer landmarksVisualizer,
            MediaPipeFingerPoseCalculator fingerPoseCalculator,
            MediaPipeTrackerStatusPreviewSender previewSender
        ) : base(settingsRepository, textureSource, mediaPipeKinematicSetter, facialValueRepository, calibrator, landmarksVisualizer)
        {
            _fingerPoseCalculator = fingerPoseCalculator;
            PreviewSender = previewSender;
        }
        
        protected override void OnStartTask()
        {
            var options = new HolisticLandmarkerOptions(
                baseOptions: new BaseOptions(
                    modelAssetPath: FilePathUtil.GetModelFilePath(ModelFileName)
                ),
                Mediapipe.Tasks.Vision.Core.RunningMode.LIVE_STREAM,
                minHandLandmarksConfidence: 0.7f,
                minFaceDetectionConfidence: 0.7f,
                minPoseDetectionConfidence: 0.6f,
                resultCallback: OnResult
            );
            _landmarker = HolisticLandmarker.CreateFromOptions(options);
        }

        protected override void OnStopTask()
        {
            ((IDisposable)_landmarker)?.Dispose();
            _landmarker = null;
        }

        protected override void OnWebCamImageUpdated(WebCamImageSource source)
        {
            if (_landmarker == null)
            {
                return;
            }
            using var image = source.BuildImage();
            // NOTE: タイミングバグを考慮して、null checkをこっちにも入れる
            _landmarker?.DetectAsync(image, source.TimestampMilliseconds);
        }

        private void OnResult(in HolisticLandmarkerResult result, Image image, long timestamp)
        {
            var hasLeftHand = result.HasLeftHandResult();
            var hasRightHand = result.HasRightHandResult();

            // NOTE: Poseの信頼性がないケースは甘めに見て通す: バストアップしか映ってないときにconfidenceが下がる可能性があるので
            if (!hasLeftHand && !hasRightHand)
            {
                MediaPipeKinematicSetter.ClearLeftHandPose();
                MediaPipeKinematicSetter.ClearRightHandPose();
                SetElbowOpenRate(result.poseLandmarks, false, false, false);    

                //LandmarksVisualizer.ClearPositions();
                //LandmarksVisualizer.Visualizer2D.Clear();
                return;
            } 
            
            var hasPose = result.poseLandmarks.landmarks is { Count: > 0 };

            if (hasLeftHand)
            {
                SetLeftHandPose(result.leftHandLandmarks, result.leftHandWorldLandmarks);
            }
            else
            {
                MediaPipeKinematicSetter.ClearLeftHandPose();
            }
            
            if (hasRightHand)
            {
                SetRightHandPose(result.rightHandLandmarks, result.rightHandWorldLandmarks);
            }
            else
            {
                MediaPipeKinematicSetter.ClearRightHandPose();
            }

            if (hasLeftHand || hasRightHand)
            {
                PreviewSender.SetHandTrackingResult(result);
            }

            SetElbowOpenRate(result.poseLandmarks, hasLeftHand, hasRightHand, hasPose);
        }

        private void VisualizeLeftHand(NormalizedLandmarks landmarks, Landmarks worldLandmarks)
        {
            LandmarksVisualizer.SetPositions(
                worldLandmarks.landmarks.Select(m => m.ToLocalPosition())
                );
            LandmarksVisualizer.Visualizer2D.SetPositions(
                landmarks.landmarks.Select(m => m.ToVector2()
                ));
        }
        
        private void SetLeftHandPose(NormalizedLandmarks landmarks, Landmarks worldLandmarks)
        {
            // 指のFK + 手首のローカル回転の取得までは下記で実施
            _fingerPoseCalculator.SetLeftHandPose(worldLandmarks);

            // 手首の位置は normalized の画像座標に基づいた値を言う。このとき、縦横のスケールだけ合わせる
            var wristLandmark = landmarks.landmarks[0];
            var normalizedPos = MediapipeMathUtil.GetTrackingNormalizePosition(wristLandmark, WebCamTextureAspect);
            var posOffset = MediapipeMathUtil.GetNormalized2DofPositionDiff(normalizedPos, Calibrator.GetCalibrationData());
            MediaPipeKinematicSetter.SetLeftHandPose(posOffset, _fingerPoseCalculator.LeftHandRotation);
        }

        private void SetRightHandPose(NormalizedLandmarks landmarks, Landmarks worldLandmarks)
        {
            _fingerPoseCalculator.SetRightHandPose(worldLandmarks);

            var wristLandmark = landmarks.landmarks[0];
            var normalizedPos = MediapipeMathUtil.GetTrackingNormalizePosition(wristLandmark, WebCamTextureAspect);
            var posOffset = MediapipeMathUtil.GetNormalized2DofPositionDiff(normalizedPos, Calibrator.GetCalibrationData());
            MediaPipeKinematicSetter.SetRightHandPose(posOffset, _fingerPoseCalculator.RightHandRotation);
        }

        //TODO: 開き具合ではなく幾何的な角度を計算するように直す。
        //(11~16 のindexを使う、という情報を保全したいので一旦commitしてるけど実装はほぼ全修正になる予定)
        private void SetElbowOpenRate(NormalizedLandmarks poseLandmarks, bool hasLeftHand, bool hasRightHand, bool hasPose)
        {
            if (!hasPose)
            {
                MediaPipeKinematicSetter.SetLeftElbowOpenRate(0f);
                MediaPipeKinematicSetter.SetRightElbowOpenRate(0f);
                return;
            }
            
            //NOTE: 手自体が検出出来てない場合、肘のRateは0扱いする
            if (hasLeftHand)
            {
                var shoulder = poseLandmarks.landmarks[11].ToVector2();
                var elbow = poseLandmarks.landmarks[13].ToVector2();
                var wrist = poseLandmarks.landmarks[15].ToVector2();
                var shoulderToElbow = (elbow - shoulder).x;
                // 画像上で左肘が肩より右にある = 肘が開いてる
                var leftElbowOpenRate = Mathf.Clamp01(Mathf.InverseLerp(
                    0.15f, 0.3f, shoulderToElbow
                ));
                MediaPipeKinematicSetter.SetLeftElbowOpenRate(leftElbowOpenRate);
            }
            else
            {
                MediaPipeKinematicSetter.SetLeftElbowOpenRate(0f);
            }
            
            if (hasRightHand)
            {
                var shoulder = poseLandmarks.landmarks[12].ToVector2();
                var elbow = poseLandmarks.landmarks[14].ToVector2();
                var wrist = poseLandmarks.landmarks[16].ToVector2();
                var diff = elbow - wrist;
                // TODO: 肘の開き判定の基準が諸説ある？
                // - そもそもElbowMotionModifierの仕様的に肘の開き度合いが直接指定できない
                // - ので、ある種のファジーさを最初から考えたほうが良さそう
                
                // 左手より左肘が左にある = 肘が開いてる
                // 肩との相対位置より筋が良い…はず
                var rightElbowOpenRate = Mathf.Clamp01(Mathf.InverseLerp(
                    0.15f, 0.3f, -diff.x
                ));
                MediaPipeKinematicSetter.SetRightElbowOpenRate(rightElbowOpenRate);
            }
            else
            {
                MediaPipeKinematicSetter.SetRightElbowOpenRate(0f);
            }
        }
    }
    
    static class HolisticLandmarkerResultExtensions
    {
        public static bool HasLeftHandResult(this in HolisticLandmarkerResult result) =>
            result.leftHandLandmarks.landmarks is { Count: > 0 } &&
            result.leftHandWorldLandmarks.landmarks is { Count : > 0 };

        public static bool HasRightHandResult(this in HolisticLandmarkerResult result) =>
            result.rightHandLandmarks.landmarks is { Count: > 0 } &&
            result.rightHandWorldLandmarks.landmarks is { Count : > 0 };
    }
}