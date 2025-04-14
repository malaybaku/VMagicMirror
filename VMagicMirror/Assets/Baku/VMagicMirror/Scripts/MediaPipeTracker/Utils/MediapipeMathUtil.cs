using System.Collections.Generic;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Tasks.Vision.FaceLandmarker;
using UnityEngine;
using Rect = Mediapipe.Tasks.Components.Containers.Rect;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    public static class MediapipeMathUtil
    {
        /// <summary>
        /// <see cref="FaceLandmarkerResult.facialTransformationMatrixes"/> から得られる行列を指定することで、
        /// カメラ座標に対する顔のローカル姿勢を求める。
        ///
        /// 戻り値の Pose.position の値がmeter単位であることは保証されてない点に注意。
        /// </summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        public static Pose GetWebCameraToFaceLocalPose(Matrix4x4 matrix)
        {
            var localPosition = matrix.GetPosition();
            var localRotation = matrix.rotation;
            var localRotEuler = localRotation.eulerAngles;
            
            // x, z:
            //   -単に「何かひっくり返ってるので直す」という観察ベースの処置。
            //   - MediapipeとMatrixの変換どっちかが理由でひっくり返っているのだが、ここでは深追いしない
            // y:
            //   - 「顔がカメラに相対している」というのを明示的に扱って空間的に正しくするために逆向きにしてる
            localRotEuler.x = -localRotEuler.x;
            localRotEuler.y += 180f;
            localRotEuler.z = -localRotEuler.z;
            localRotation = Quaternion.Euler(localRotEuler);

            return new Pose(localPosition, localRotation);
        }

        /// <summary>
        /// <see cref="GetWebCameraToFaceLocalPose"/> の結果を渡すことで、逆に「顔から見たカメラのローカル姿勢」を求める。
        /// </summary>
        /// <returns></returns>
        public static Pose GetInvertedPose(Pose pose)
        {
            var rotInverse = Quaternion.Inverse(pose.rotation);
            return new Pose(rotInverse * (-pose.position), rotInverse);
        }

        // TODO: !data.HasCameraLocalPose の場合どうする？ここではケアしないで呼び出し元に任す？
        /// <summary>
        /// キャリブレーション結果も考慮して、顔の現在の姿勢オフセットを取得する。
        /// positionはスケールがかかってないので (大まかに cm -> m くらいで) 変換して使うのが期待値。
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Pose GetCalibratedFaceLocalPose(Matrix4x4 matrix, CameraCalibrationData data)
        {
            // 「キャリブ時点での顔 -> カメラ -> 現在の顔」というヒエラルキー的な構造で変換していく
            var calibratedFaceToCamera = data.CameraLocalPose;
            var cameraToFace = GetWebCameraToFaceLocalPose(matrix);

            return new Pose(
                calibratedFaceToCamera.rotation * cameraToFace.position + calibratedFaceToCamera.position,
                calibratedFaceToCamera.rotation * cameraToFace.rotation
            );
        }

        // landmark.x が小さい = カメラ画像の左側 = 空間的には右 = ユーザー座標では +x
        // landmark.y が小さい = カメラ画像の上側 = 空間的には上 = ユーザー座標では +y
        // keypointも同じ考え方で変換される
        public static Vector2 GetTrackingNormalizePosition(NormalizedLandmark landmark, float webcamTextureAspect)
        {
            return new Vector2(
                0.5f - landmark.x,
                (0.5f - landmark.y) / webcamTextureAspect
            );
        }
        
        public static Vector2 GetTrackingNormalizePosition(NormalizedKeypoint keypoint, float webCamTextureAspect)
        {
            return new Vector2(
                0.5f - keypoint.x,
                (0.5f - keypoint.y) / webCamTextureAspect
            );
        }

        public static (Vector2 leftBottom, Vector2 rightTop) GetNormalizedPointsFromBound(
            Rect bound, float webCamTextureWidth, float webCamTextureHeight)
        {
            var left = bound.left / webCamTextureWidth;
            var right = bound.right / webCamTextureWidth;
            var top = bound.top / webCamTextureHeight;
            var bottom = bound.bottom / webCamTextureHeight;
            
            // NOTE:
            // - 「画像の右 = 空間上は左」になるので下記のように left/right がひっくり返る
            // - 画像の中心を中心扱いしたいので、aspectInverseをかけるタイミングに注意 (そのまんまのtop/bottomは画像の上端が中心になっている)
            var aspectInverse = webCamTextureHeight / webCamTextureWidth;
            var leftBottom = new Vector2(0.5f - right, (0.5f - bottom) * aspectInverse);
            var rightTop = new Vector2(0.5f - left, (0.5f - top) * aspectInverse);
            return (leftBottom, rightTop);
        }
        
        /// <summary>
        /// キャリブレーション結果を考慮して、指定した画像座標が顔の基準位置からどのくらいズレてるかを取得する。
        /// 座標系のスケールは画像座標のままで、カメラから見て右が +x, 上が +y になるように扱われる
        /// </summary>
        /// <param name="normalizedPoint"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Vector2 GetNormalized2DofPositionDiff(Vector2 normalizedPoint, CameraCalibrationData data)
        {
            return normalizedPoint - data.FaceCenterNormalizedPosition;
        }
        
        // NOTE: 厳密にはこの辺はMediapipeと関係ないけど、クラス分けるほどでもないのでここで定義している
        public static Quaternion VrmLeftHandForwardRotation { get; } = Quaternion.Euler(0, 90, -90);
        public static Quaternion VrmRightHandForwardRotation { get; } = Quaternion.Euler(0, -90, 90);
        
        public static Quaternion GetVrmForwardHandRotation(bool isLeftHand) => isLeftHand
            ? VrmLeftHandForwardRotation
            : VrmRightHandForwardRotation;
        

    }
}