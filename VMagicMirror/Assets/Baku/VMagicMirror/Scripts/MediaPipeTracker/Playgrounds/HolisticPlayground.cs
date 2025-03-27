using System;
using System.Linq;
using Mediapipe;
using Mediapipe.Tasks.Components.Containers;
using UnityEngine;
using Mediapipe.Tasks.Core;
using Mediapipe.Tasks.Vision.HolisticLandmarker;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    public class HolisticPlayground : MediapipeTaskRunnerBase
    {
        private const string ModelFileName = "holistic_landmarker.bytes";

        // カメラ映像の左端～右端まで手が動いたときにアバターの手の移動幅として反映する距離。HandPlaygroundと同じやつ
        [SerializeField] private float handOffsetScale = 0.6f;

        protected float HandOffsetScale => handOffsetScale;

        private float ArmDistance => KinematicSetter.BodyScaleCalculator.ArmLength * 0.7f;

        private HolisticLandmarker _landmarker = null;
        private HandPoseSetter _handPoseSetter;
        private readonly Vector3[] _linePosCache = new Vector3[PoseLineIndexesLength];

        private static readonly int PoseLineIndexesLength;

        // NOTE: マイナスの値は下記の様に使う。あくまでビジュアライズ向けです
        // -1 = 9,10の中心 = 口
        // -2 = 9,10,11,12の中心 = 首の位置
        // -3 = 11,12の中心 = 鎖骨の真ん中らへん
        // -4 = 23,24の中心 = hipsらへん
        private static readonly int[] PoseLineIndexes = new[]
        {
            // 頭らへん(肩も含む)
            8, 5, 0, 2, 7, 2, 0, -1, 10, 9, -1, -2, -3,
            // 右手
            11, 13, 15, 17, 19, 15, 13, 11, -3,
            // 左手
            12, 14, 16, 18, 20, 16, 14, 12, -3, -4,
            // 右足
            24, 26, 28, 26, 24, -4,
            // 左足
            23, 25, 27, 25, 23,
        };
        
        static HolisticPlayground()
        {
            PoseLineIndexesLength = PoseLineIndexes.Length;
        }
        
        protected override void OnStartTask()
        {
            _handPoseSetter ??= new HandPoseSetter(KinematicSetter);

            var options = new HolisticLandmarkerOptions(
                baseOptions: new BaseOptions(
                    modelAssetPath: FilePathUtil.GetModelFilePath(ModelFileName)
                ),
                Mediapipe.Tasks.Vision.Core.RunningMode.LIVE_STREAM,
                resultCallback: OnHolisticLandmarkResult
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
            using var image = source.BuildImage();
            _landmarker.DetectAsync(image, source.TimestampMilliseconds);
        }

        private void OnHolisticLandmarkResult(in HolisticLandmarkerResult result, Image image, long timestamp)
        {
            // NOTE: faceのデータは使わない (VMM本体で使う想定ではない) ので捨ててしまってOK…です…よね…？
            var poseLandmarkCount = result.poseWorldLandmarks.landmarks?.Count ?? -1;

            // 全身がちっとも取れてない == トラッキングロスト
            if (poseLandmarkCount <= 0)
            {
                KinematicSetter.ClearLeftHandPose();
                KinematicSetter.ClearRightHandPose();
                LandmarksVisualizer.ClearPositions();
                LandmarksVisualizer.ClearLinePositions();
                return;
            }
            
            VisualizePoseLandmarks(result.poseWorldLandmarks);
            VisualizePoseAsLine(result.poseWorldLandmarks);
            
            // 「全身姿勢は出てるけど手は画面内に収まってない」みたいなケースは(当然)あることに注意
            var leftHandLandmarkCount = result.leftHandWorldLandmarks.landmarks?.Count ?? -1;
            if (leftHandLandmarkCount > 0)
            {
                SetLeftHandPose(result.leftHandLandmarks, result.leftHandWorldLandmarks);
            }
            else
            {
                KinematicSetter.ClearLeftHandPose();
            }

            var rightHandLandmarkCount = result.rightHandWorldLandmarks.landmarks?.Count ?? -1;
            if (rightHandLandmarkCount > 0)
            {
                SetRightHandPose(result.rightHandLandmarks, result.rightHandWorldLandmarks);
            }
            else
            {
                KinematicSetter.ClearRightHandPose();
            }
        }

        private void VisualizePoseLandmarks(Landmarks poseWorldLandmarks)
        {
            LandmarksVisualizer.SetPositions(
                poseWorldLandmarks
                    .landmarks
                    .Select(m => m.ToLocalPosition())
                    .ToList()
                );
        }
        
        private void SetLeftHandPose(NormalizedLandmarks landmarks, Landmarks worldLandmarks)
        {
            // 指のFK + 手首のローカル回転の取得はここでやる
            _handPoseSetter.SetLeftHandPose(worldLandmarks);

            // TODO: コードとして復活させる場合、HandPlaygroundを真似してね
            return;
        }

        private void SetRightHandPose(NormalizedLandmarks landmarks, Landmarks worldLandmarks)
        {
            // do nothing
        }
        
        private void VisualizePoseAsLine(Landmarks poseWorldLandmarks)
        {
            var arr = poseWorldLandmarks.landmarks;

            for (var i = 0; i < _linePosCache.Length; i++)
            {
                var index = PoseLineIndexes[i];
                switch (index)
                {
                    case -1:
                        _linePosCache[i] = 0.5f * (arr[9].ToLocalPosition() + arr[10].ToLocalPosition());
                        break;
                    case -2:
                        _linePosCache[i] = 0.25f * (
                            arr[9].ToLocalPosition() + arr[10].ToLocalPosition() + 
                            arr[11].ToLocalPosition() + arr[12].ToLocalPosition()
                            );
                        break;
                    case -3:
                        _linePosCache[i] = 0.5f * (arr[11].ToLocalPosition() + arr[12].ToLocalPosition());
                        break;
                    case -4:
                        _linePosCache[i] = 0.5f * (arr[23].ToLocalPosition() + arr[24].ToLocalPosition());
                        break;
                    default:
                        _linePosCache[i] = arr[index].ToLocalPosition();
                        break;
                }
            }
            LandmarksVisualizer.SetLinePositions(_linePosCache);
        }

        private Quaternion EstimateChestRotation(Landmarks poseWorldLandmarks)
        {
            var marks = poseWorldLandmarks.landmarks;
            var hipsCenter = 0.5f * (marks[23].ToLocalPosition() + marks[24].ToLocalPosition());

            var leftShoulder = marks[11].ToLocalPosition();
            var rightShoulder = marks[24].ToLocalPosition();
            var shoulderCenter = 0.5f * (leftShoulder + rightShoulder);
            
            var upward = (shoulderCenter - hipsCenter).normalized;
            var forward = Vector3.Cross(
                rightShoulder - hipsCenter,
                leftShoulder - hipsCenter
            ).normalized;

            return Quaternion.LookRotation(forward, upward);
        }
    }
}