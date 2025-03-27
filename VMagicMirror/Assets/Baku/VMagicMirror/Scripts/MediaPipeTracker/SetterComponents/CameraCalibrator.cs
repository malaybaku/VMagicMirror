using System;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Tasks.Vision.FaceLandmarker;
using UnityEngine;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    public class CameraCalibrator : MonoBehaviour
    {
        private CameraCalibrationData _calibrationData = CameraCalibrationData.Empty();
        private readonly object _calibrationDataLock = new();

        // NOTE: プロパティ形式でもよいが、lockもやってるので仰々しく…
        public CameraCalibrationData GetCalibrationData()
        {
            lock (_calibrationDataLock)
            {
                return _calibrationData;
            }
        }
        
        public bool TrySetTwoDofData(DetectionResult result)
        {
            if (result.detections is not { Count: > 0 })
            {
                return false;
            }

            var detect = result.detections[0];

            // 鼻先を顔の中心と見なす
            // NOTE: これ以外だとboundの中心を取る方法がある
            // TODO: xyの正負とか範囲に注意な！
            var noseKeyPoint = detect.keypoints[2];
            var faceCenterNormalizedPosition = new Vector2(noseKeyPoint.x, 1f - noseKeyPoint.y);
            Debug.Log($"[2dof] nose pos={faceCenterNormalizedPosition.x:0.00}, {faceCenterNormalizedPosition.y:0.00}");

            lock (_calibrationDataLock)
            {
                _calibrationData = _calibrationData.WithTwoDoF(faceCenterNormalizedPosition);
            }
            return true;
        }

        public bool TrySetSixDofData(FaceLandmarkerResult result)
        {
            if (result.facialTransformationMatrixes is not { Count: > 0 })
            {
                // NOTE: 顔が検出できてない場合以外だと、タスク作成のオプションがミスってる場合もガードに引っかかる
                return false;
            }

            var matrix = result.facialTransformationMatrixes[0];
            // カメラから見た顔の位置: 直感的な値なので、まずはコッチを取得する(& デバッグ上必要ならビジュアライズ)
            var cameraToFacePose = MediapipeMathUtil.GetWebCameraToFaceLocalPose(matrix);
            {
                //debug
                var pos = cameraToFacePose.position;
                var rot = cameraToFacePose.rotation.eulerAngles;
                Debug.Log($"[6dof] camera to face pos = {pos.x:0.000}, {pos.y:0.000}, {pos.z:0.000}");
                Debug.Log($"[6dof] camera to face rot = {rot.x:0.000}, {rot.y:0.000}, {rot.z:0.000}");
            }

            // 顔から見たカメラの位置: これが実際にはキャリブレーションデータとして保存される
            var faceToCameraPose = MediapipeMathUtil.GetInvertedPose(cameraToFacePose);
            
            // NOTE: matrixesがあるのでlandmarkも必ず存在する前提で処理してしまう
            var noseLandmark = result.faceLandmarks[0].landmarks[0];
            var faceCenterNormalizedPosition = new Vector2(1f - noseLandmark.x, 1f - noseLandmark.y);
            Debug.Log($"[6dof] nose pos={faceCenterNormalizedPosition.x:0.00}, {faceCenterNormalizedPosition.y:0.00}");

            
            lock (_calibrationDataLock)
            {
                _calibrationData = CameraCalibrationData.SixDoF(faceCenterNormalizedPosition, faceToCameraPose);
            }

            return true;
        }

       
    }

}
