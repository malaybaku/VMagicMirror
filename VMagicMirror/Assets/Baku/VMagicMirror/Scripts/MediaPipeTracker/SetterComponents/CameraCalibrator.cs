using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Tasks.Vision.FaceLandmarker;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    public class CameraCalibrator
    {
        [Inject]
        public CameraCalibrator(MediaPipeTrackerSettingsRepository repository)
        {
            _repository = repository;
        }
    
        private readonly MediaPipeTrackerSettingsRepository _repository;

        public CameraCalibrationData GetCalibrationData() => _repository.CurrentCalibrationData;
        
        public bool TrySetTwoDofData(DetectionResult result, float webCamTextureAspect)
        {
            if (result.detections is not { Count: > 0 })
            {
                return false;
            }

            var detect = result.detections[0];

            // 鼻先を顔の中心と見なす。これ以外だとboundの中心を取る方法があるが、どっちも一長一短なので鼻でやっている
            var noseKeyPoint = detect.keypoints[2];
            var faceCenterNormalizedPosition =
                MediapipeMathUtil.GetTrackingNormalizePosition(noseKeyPoint, webCamTextureAspect);
            
            var currentData = _repository.CurrentCalibrationData;
            _repository.SetCalibrationResult(currentData.WithTwoDoF(faceCenterNormalizedPosition));
            return true;
        }

        public bool TrySetSixDofData(FaceLandmarkerResult result, float webCamTextureAspect)
        {
            if (result.facialTransformationMatrixes is not { Count: > 0 })
            {
                // NOTE: 顔が検出できてない場合以外だと、タスク作成のオプションがミスってる場合もガードに引っかかる
                return false;
            }

            var matrix = result.facialTransformationMatrixes[0];
            // カメラから見た顔の位置: 直感的な値なので、まずはコッチを取得する(& デバッグ上必要ならビジュアライズ)
            var cameraToFacePose = MediapipeMathUtil.GetWebCameraToFaceLocalPose(matrix);

            // 顔から見たカメラの位置: これが実際にはキャリブレーションデータとして保存される
            var faceToCameraPose = MediapipeMathUtil.GetInvertedPose(cameraToFacePose);
            
            // NOTE: matrixesがあるのでlandmarkも必ず存在する前提
            var noseLandmark = result.faceLandmarks[0].landmarks[0];
            var faceCenterNormalizedPosition
                = MediapipeMathUtil.GetTrackingNormalizePosition(noseLandmark, webCamTextureAspect);

            _repository.SetCalibrationResult(
                CameraCalibrationData.SixDoF(faceCenterNormalizedPosition, faceToCameraPose)
            );
            return true;
        }
    }
}
