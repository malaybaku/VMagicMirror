using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mediapipe;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Tasks.Core;
using Mediapipe.Tasks.Vision.FaceDetector;
using Zenject;
using Rect = Mediapipe.Tasks.Components.Containers.Rect;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    // TODO: v4.0.0の実装方針ではこのクラス使わないかも…で、ホントに使わない場合は削除してよい
    // iFacialMocap + ハンドトラッキングの場合、キャリブレーションの瞬間だけFace (Detector|Landmarker)が動いてれば用が足りるので、
    // 実際に使わなさそうだったら使わない方向に寄せる
    public class HandAndFaceDetectorTask : HandTask
    {
        private bool useInterlace = false;
        private bool requestCalibration;

        private float yawMax = 30f;
        private float yawMaxPositionRate = 0.8f;
        
        private float pitchMax = 30f;
        private float pitchMaxPositionRate = 0.8f;
        
        private const string FaceModelFileName = "blaze_face_short_range.bytes";

        private FaceDetector _detector;
        private int _interlaceCount;

        [Inject]
        public HandAndFaceDetectorTask(
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
            base.OnStartTask();
            
            var options = new FaceDetectorOptions(
                baseOptions: new BaseOptions(
                    modelAssetPath: FilePathUtil.GetModelFilePath(FaceModelFileName)
                ),
                Mediapipe.Tasks.Vision.Core.RunningMode.LIVE_STREAM,
                numFaces: 1,
                resultCallback: OnResult 
            );
            _detector = FaceDetector.CreateFromOptions(options);
        }

        protected override void OnStopTask()
        {
            base.OnStopTask();

            ((IDisposable)_detector)?.Dispose();
            _detector = null;
        }

        protected override void OnWebCamImageUpdated(WebCamImageSource source)
        {
            if (useInterlace)
            {
                // インターレースあり: 1枚ずつ交代でHand / Faceで使う
                _interlaceCount = (_interlaceCount + 1) % 2;
                if (_interlaceCount == 0)
                {
                    base.OnWebCamImageUpdated(source);
                }
                else
                {
                    using var image = source.BuildImage();
                    _detector.DetectAsync(image, source.TimestampMilliseconds);
                }
            }
            else
            {
                // インターレース無し: 1つのテクスチャから二重に画像を作って2タスクで使う
                base.OnWebCamImageUpdated(source);

                using var image = source.BuildImage();
                _detector.DetectAsync(image, source.TimestampMilliseconds);
            }
        }
        
        private void OnResult(DetectionResult result, Image image, long timestamp)
        {
            LogOnResultCalled(timestamp);
            if (result.detections is not { Count: > 0 })
            {
                KinematicSetter.ClearHeadPose();
                LandmarksVisualizer.Visualizer2D.Clear();
                return;
            }

            var detect = result.detections[0];
            var bound = detect.boundingBox;
            
            // これはデバッグ表示してるだけ
            LandmarksVisualizer.Visualizer2D.SetPositions(
                detect.keypoints.Select(kp => new Vector2(kp.x, kp.y))
                    .Concat(GetNormalizedPointsFromBound(bound))
                );

            // NOTE: noseと比べてもboundsは結構ブレる印象があるので使わないことにしている
            // var normalizedBoundCenter = new Vector2(
            //     (bound.left + bound.right) * 0.5f / WebCamTextureWidth,
            //     (bound.top + bound.bottom) * 0.5f / WebCamTextureHeight
            // );

            var calibrationData = Calibrator.GetCalibrationData();
            
            // NOTE: noseだけはワールド位置に関するので縮尺を考慮する。それ以外は幾何的な角度計算に使うので、アスペクト比の影響だけ除去する (これはboundも同じ)
            var nosePos = NosePos(detect.keypoints, calibrationData);

            var positions = new KeyPointPositions(
                detect.keypoints, bound, 
                WebCamTextureWidth, WebCamTextureHeight, WebCamTextureAspect
                );

            var headAngle = Quaternion.Euler(
                EstimatePitchAngle(positions),
                EstimateYawAngle(positions),
                EstimateRollAngle(positions)
                );
            
            // NOTE: 低負荷顔トラッキングとしてもMediaPipeを使う機運になったらここを復活させる
            //KinematicSetter.SetHeadPose2Dof(nosePos, headAngle);
            
            if (requestCalibration)
            {
                var calibrationSuccess = Calibrator.TrySetTwoDofData(result);
                Debug.Log($"2dof calibration, success?={calibrationSuccess}");
                // NOTE: ホントはマルチスレッドからアクセスしないほうがヨイが、まあデバッグ表示なので…
                requestCalibration = false;
            }
        }

        private float EstimateRollAngle(KeyPointPositions positions)
        {
            var diff = (positions.LeftEye + positions.LeftEar - positions.RightEye - positions.RightEar);
            return -Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        }

        private float EstimateYawAngle(KeyPointPositions positions)
        {
            // 考え方: 顔の正面付近の特徴点が全体としてboundsの右側に寄ってれば右を向いていると判断する (※が、何か符号が怪しいので反転してます)
            var meanX = 0.25f * (positions.LeftEye.x + positions.RightEye.x + positions.Nose.x + positions.Mouth.x);
            var rawPositionRate = Mathf.InverseLerp(positions.BoundsLeftBottom.x, positions.BoundsRightTop.x, meanX);
            var yawRate = 2f * Mathf.InverseLerp(1f - yawMaxPositionRate, yawMaxPositionRate, rawPositionRate) - 1f;
            return -yawMax * yawRate;
        }

        private float EstimatePitchAngle(KeyPointPositions positions)
        {
            // 考え方: 顔の正面付近の特徴点が全体としてboundsの上/下に寄ってれば上/下を向いていると判断する
            var meanY = 0.25f * (positions.LeftEye.y + positions.RightEye.y + positions.Nose.y + positions.Mouth.y);
            var rawPositionRate = Mathf.InverseLerp(positions.BoundsLeftBottom.y, positions.BoundsRightTop.y, meanY);
            Debug.Log($"rawPositionRate: {rawPositionRate:0.000}");
            var pitchRate = 2f * Mathf.InverseLerp(1f - pitchMaxPositionRate, pitchMaxPositionRate, rawPositionRate) - 1f;
            
            // 上向きはピッチとしてはマイナスなことに注意
            return -pitchMax * pitchRate;
        }
        
        private Vector2 NosePos(List<NormalizedKeypoint> keypoints, CameraCalibrationData calibrationData)
        {
            var keypoint = keypoints[2];
            var normalized = MediapipeMathUtil.GetTrackingNormalizePosition(keypoint, WebCamTextureAspect);
            return MediapipeMathUtil.GetNormalized2DofPositionDiff(normalized, calibrationData);
        }
        
        private Vector2[] GetNormalizedPointsFromBound(Rect bound)
        {
            var factor = new Vector2(1f / WebCamTextureWidth, 1f / WebCamTextureHeight);
            var result = new Vector2[4];
            result[0] = Vector2.Scale(new Vector2(bound.left, bound.top), factor);
            result[1] = Vector2.Scale(new Vector2(bound.right, bound.top), factor);
            result[2] = Vector2.Scale(new Vector2(bound.left, bound.bottom), factor);
            result[3] = Vector2.Scale(new Vector2(bound.right, bound.bottom), factor);
            return result;
        }
        
        // 画像座標系の左右・上下の反転とアスペクト比の影響をケアしたkeypoint一覧のデータ。
        // 位置はすべて画像の横方向のサイズでおよそ[0,1]に正規化され、右が +x, 上が +y
        readonly struct KeyPointPositions
        {
            public readonly Vector2 BoundsLeftBottom;
            public readonly Vector2 BoundsRightTop;
            
            public readonly Vector2 LeftEye;
            public readonly Vector2 RightEye;
            public readonly Vector2 Nose;
            public readonly Vector2 Mouth;
            public readonly Vector2 LeftEar;
            public readonly Vector2 RightEar;
            
            // NOTE: width, height, aspectを全部もらうのは冗長だが、再計算するほどでもないので全部貰っている
            public KeyPointPositions(
                List<NormalizedKeypoint> keypoints, Rect bound,
                float textureWidth, float textureHeight, float textureAspect
                )
            {
                (BoundsLeftBottom, BoundsRightTop) =
                    MediapipeMathUtil.GetNormalizedPointsFromBound(bound, textureWidth, textureHeight);
                
                LeftEye = MediapipeMathUtil.GetTrackingNormalizePosition(keypoints[0], textureAspect);
                RightEye = MediapipeMathUtil.GetTrackingNormalizePosition(keypoints[1], textureAspect);
                Nose = MediapipeMathUtil.GetTrackingNormalizePosition(keypoints[2], textureAspect);
                Mouth = MediapipeMathUtil.GetTrackingNormalizePosition(keypoints[3], textureAspect);
                LeftEar = MediapipeMathUtil.GetTrackingNormalizePosition(keypoints[4], textureAspect);
                RightEar = MediapipeMathUtil.GetTrackingNormalizePosition(keypoints[5], textureAspect);
            }
        }
    }
}