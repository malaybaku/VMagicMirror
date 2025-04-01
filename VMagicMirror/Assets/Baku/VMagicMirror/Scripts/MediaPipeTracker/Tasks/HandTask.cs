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

        private HandLandmarker _landmarker;
        private HandPoseSetter _handPoseSetter;

        [Inject]
        public HandTask(
            MediaPipeTrackerSettingsRepository settingsRepository,
            WebCamTextureSource textureSource,
            KinematicSetter kinematicSetter, 
            FacialSetter facialSetter,
            CameraCalibrator calibrator,
            LandmarksVisualizer landmarksVisualizer
        ) : base(settingsRepository, textureSource, kinematicSetter, facialSetter, calibrator, landmarksVisualizer)
        {
        }
        
        protected override void OnStartTask()
        {
            _handPoseSetter ??= new HandPoseSetter(KinematicSetter);
            
            var options = new HandLandmarkerOptions(
                baseOptions: new BaseOptions(
                    modelAssetPath: FilePathUtil.GetModelFilePath(ModelFileName)
                ),
                Mediapipe.Tasks.Vision.Core.RunningMode.LIVE_STREAM,
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
            using var image = source.BuildImage();
            _landmarker.DetectAsync(image, source.TimestampMilliseconds);
        }

        private void OnResult(HandLandmarkerResult result, Image image, long timestamp)
        {
            if (result.handedness == null || 
                result.handLandmarks == null ||
                result.handWorldLandmarks == null)
            {
                KinematicSetter.ClearLeftHandPose();
                KinematicSetter.ClearRightHandPose();

                //LandmarksVisualizer.ClearPositions();
                //LandmarksVisualizer.Visualizer2D.Clear();
                return;
            }

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
                KinematicSetter.ClearLeftHandPose();
            }

            if (!hasRightHand)
            {
                KinematicSetter.ClearRightHandPose();
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
            _handPoseSetter.SetLeftHandPose(worldLandmarks);

            // 手首の位置は normalized の画像座標に基づいた値を言う。このとき、縦横のスケールだけ合わせる
            var wristLandmark = landmarks.landmarks[0];
            var normalizedPos = MediapipeMathUtil.GetTrackingNormalizePosition(wristLandmark, WebCamTextureAspect);
            var posOffset = MediapipeMathUtil.GetNormalized2DofPositionDiff(normalizedPos, Calibrator.GetCalibrationData());
            KinematicSetter.SetLeftHandPose(posOffset, _handPoseSetter.LeftHandRotation);
        }

        private void SetRightHandPose(NormalizedLandmarks landmarks, Landmarks worldLandmarks)
        {
            _handPoseSetter.SetRightHandPose(worldLandmarks);

            var wristLandmark = landmarks.landmarks[0];
            var normalizedPos = MediapipeMathUtil.GetTrackingNormalizePosition(wristLandmark, WebCamTextureAspect);
            var posOffset = MediapipeMathUtil.GetNormalized2DofPositionDiff(normalizedPos, Calibrator.GetCalibrationData());
            KinematicSetter.SetRightHandPose(posOffset, _handPoseSetter.RightHandRotation);
        }
    }
}