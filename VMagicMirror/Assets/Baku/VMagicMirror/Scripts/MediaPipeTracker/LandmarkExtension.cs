using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Tasks.Vision.FaceLandmarker;
using UnityEngine;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    public static class LandmarkExtension
    {
        // NOTE: 
        // x: カメラから見た座標系になることで左右がひっくり返ってるので -1 倍する
        // y: Mediapipeの座標系ではy軸が下向きなので -1 倍する
        // z: Mediapipeの座標系ではカメラに近づく方向がマイナスなので -1 倍する
        public static Vector3 ToLocalPosition(this Landmark m)
            //=> Mediapipe.Unity.CoordinateSystem.RealWorldCoordinate.RealWorldToLocalPoint(m.x, m.y, m.z);
            => new(-m.x, -m.y, -m.z);

        /// <summary>
        /// ランドマークの座標をそのままVector2に変換する。値の変換はとくに絡まないことに注意
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public static Vector2 ToVector2(this NormalizedLandmark m) => new(m.x, m.y);

    }

    public static class FaceLandmarkerResultExtension
    {
        /// <summary>
        /// FaceLandmarkerの結果が有効なら鼻のランドマークを取得しつつtrueを返す
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool TryGetNoseNormalizedLandmark(
            this FaceLandmarkerResult result, out NormalizedLandmark normalizedLandmark)
        {
            if (result.faceLandmarks is { Count: > 0 } faceLandmarks &&
                faceLandmarks[0].landmarks is { Count: > 1 } landmarks)
            {
                normalizedLandmark = landmarks[1];
                return true;
            }

            normalizedLandmark = default;
            return true;
        }
    }
}
