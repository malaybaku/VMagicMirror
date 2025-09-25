using System;
using System.Linq;
using Mediapipe;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Tasks.Core;
using Mediapipe.Tasks.Vision.HolisticLandmarker;
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
            if (result.leftHandLandmarks.landmarks == null &&
                result.rightHandLandmarks.landmarks == null)
            {
                MediaPipeKinematicSetter.ClearLeftHandPose();
                MediaPipeKinematicSetter.ClearRightHandPose();

                //LandmarksVisualizer.ClearPositions();
                //LandmarksVisualizer.Visualizer2D.Clear();
                return;
            }

            // TODO: Poseも要求するかどうか。Poseがない場合をOKにすると肘の位置がわからないケースが出てくるのがネック
            // TODO: そもそもヒジの位置をKinematicSetterに渡す仕組みがまだない

            var hasLeftHand = false;
            var hasRightHand = false;
            if (result.leftHandLandmarks.landmarks != null && result.leftHandWorldLandmarks.landmarks != null)
            {
                hasLeftHand = true;
                SetLeftHandPose(result.leftHandLandmarks, result.leftHandWorldLandmarks);
            }
            
            if (result.rightHandLandmarks.landmarks != null && result.rightHandWorldLandmarks.landmarks != null)
            {
                hasRightHand = true;
                SetRightHandPose(result.rightHandLandmarks, result.rightHandWorldLandmarks);
            }

            if (!hasLeftHand)
            {
                MediaPipeKinematicSetter.ClearLeftHandPose();
            }

            if (!hasRightHand)
            {
                MediaPipeKinematicSetter.ClearRightHandPose();
            }

            if (hasLeftHand || hasRightHand)
            {
                PreviewSender.SetHandTrackingResult(result);
            }
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
    }
}