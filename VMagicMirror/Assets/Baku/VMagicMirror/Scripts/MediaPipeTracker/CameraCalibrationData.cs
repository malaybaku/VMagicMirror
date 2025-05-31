using UnityEngine;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    public readonly struct CameraCalibrationData
    {
        public CameraCalibrationData(
            Vector2 faceCenterNormalizedPosition, 
            bool hasCameraLocalPose,
            Pose cameraLocalPose)
        {
            FaceCenterNormalizedPosition = faceCenterNormalizedPosition;
            HasCameraLocalPose = hasCameraLocalPose;
            CameraLocalPose = cameraLocalPose;
        }
        
        /// <summary>
        /// 顔の中心(鼻の先頭とか)を画像座標で表したもの
        /// 左: x = -0.5
        /// 右: x = +0.5
        /// 下: y = -0.5 / Webカメラのアス比
        /// 上: y = +0.5 / Webカメラのアス比 
        /// </summary>
        public Vector2 FaceCenterNormalizedPosition { get; }
        
        /// <summary>
        /// <see cref="CameraLocalPose"/> が有効な値かどうか。無効な場合、テキトーな
        /// </summary>
        public bool HasCameraLocalPose { get; }
        
        /// <summary>
        /// 基準位置の顔を原点姿勢としたときの、カメラのローカル姿勢
        /// Positionの数値は FaceLandmarker の Matrix4x4 に基づく値で、有次元化する前の値(だいたいcm単位くらいの値)が入る
        /// </summary>
        public Pose CameraLocalPose { get; }
        
        /// <summary>
        /// 2DoFのキャリブレーションだけ行った場合に使う。
        /// このメソッドを呼んだ場合、6DoFのキャリブレーションデータは据え置きになる
        /// </summary>
        /// <param name="faceCenterNormalizedPosition"></param>
        /// <returns></returns>
        public CameraCalibrationData WithTwoDoF(Vector2 faceCenterNormalizedPosition)
            => new(faceCenterNormalizedPosition, HasCameraLocalPose, CameraLocalPose);

        public static CameraCalibrationData TwoDoF(Vector2 faceCenterNormalizedPosition)
            => new(faceCenterNormalizedPosition, false, Pose.identity);

        public static CameraCalibrationData SixDoF(Vector2 faceCenterNormalizedPosition, Pose faceToCameraLocalPose)
            => new(faceCenterNormalizedPosition, true, faceToCameraLocalPose);
        
        public static CameraCalibrationData Empty()
            => new(Vector2.zero, false, DefaultCameraLocalPose());

        // 書いてるとおりだが、顔の真正面50cmにカメラがある状態を便宜的に基準にしておく
        private static Pose DefaultCameraLocalPose()
            => new(new Vector3(0, 0, 50f), Quaternion.Euler(0, 180f, 0));
    }
}
