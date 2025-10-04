using System;
using System.Linq;
using UnityEngine;
using Mediapipe;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Tasks.Core;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Zenject;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    // NOTE: 「外部トラッキング(≒iFacialMocap) + ハンドトラッキング」の場合にだけ有効になる
    public class HandTask : MediaPipeTrackerTaskBase
    {
        // NOTE: MediaPipeの標準的なモデルデータでは hand_landmark_(full|lite) という名称のものもあるが、これはlegacyのデータなのか動作しない 
        private const string ModelFileName = "hand_landmarker.bytes";
        private const string LeftHandHandednessName = "Left";
        private const string RightHandHandednessName = "Right";

        protected MediaPipeTrackerStatusPreviewSender PreviewSender { get; }

        private readonly MediaPipeFingerPoseCalculator _fingerPoseCalculator;
        private HandLandmarker _landmarker;

        [Inject]
        public HandTask(
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
            var options = new HandLandmarkerOptions(
                baseOptions: new BaseOptions(
                    modelAssetPath: FilePathUtil.GetModelFilePath(ModelFileName)
                ),
                Mediapipe.Tasks.Vision.Core.RunningMode.LIVE_STREAM,
                minHandDetectionConfidence: 0.7f,
                minHandPresenceConfidence: 0.7f,
                minTrackingConfidence: 0.7f,
                numHands: 2,
                resultCallback: OnResult
            );
            _landmarker = HandLandmarker.CreateFromOptions(options);
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

        private void OnResult(HandLandmarkerResult result, Image image, long timestamp)
        {
            if (result.handedness == null || 
                result.handLandmarks == null ||
                result.handWorldLandmarks == null)
            {
                MediaPipeKinematicSetter.ClearLeftHandPose();
                MediaPipeKinematicSetter.ClearRightHandPose();

                //LandmarksVisualizer.ClearPositions();
                //LandmarksVisualizer.Visualizer2D.Clear();
                return;
            }

            // NOTE: 手トラッキングしながら肘トラをon->offに切り替えたとき用に、明示的に切り続けておく
            MediaPipeKinematicSetter.SetLeftShoulderToElbow(null);
            MediaPipeKinematicSetter.SetRightShoulderToElbow(null);
            
            var hasLeftHand = false;
            var hasRightHand = false;
            for (var i = 0; i < result.handedness.Count; i++)
            {
                // categoryが無い可能性は考慮しない
                var categoryName = result.handedness[i].categories[0].categoryName;
                switch (categoryName)
                {
                    case LeftHandHandednessName:
                        SetLeftHandPose(result.handLandmarks[i], result.handWorldLandmarks[i]);
                        hasLeftHand = true;
                        break;
                    case RightHandHandednessName:
                        SetRightHandPose(result.handLandmarks[i], result.handWorldLandmarks[i]);
                        hasRightHand = true;
                        break;
                    default:
                        // 来ないはず
                        Debug.LogWarning("Detect Unknown Handedness type by HandLandmarker");
                        break;
                }
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