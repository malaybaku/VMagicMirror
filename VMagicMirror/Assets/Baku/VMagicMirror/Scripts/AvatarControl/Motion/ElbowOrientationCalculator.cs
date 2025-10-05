using UnityEngine;

namespace Baku.VMagicMirror
{
    public static class ElbowOrientationCalculator
    {
        // 画像上で肘と肩の位置が近ければ肘IKの位置を前方に突き出す」という処置をするための画像上の距離の上下限
        private const float ConvertToForwardMagnitudeMax = 0.2f;
        private const float ConvertToForwardMagnitudeMin = 0.1f;
        
        /// <summary>
        /// 画像座標のスケールで左肩～左肘の差分ベクトルを受け取って、アバターの左肘の向きベクトルを計算する。戻り値は単位ベクトルになる
        /// </summary>
        /// <param name="shoulderToElbow"></param>
        /// <returns></returns>
        public static Vector3 CalculateLeftElbowDirection(Vector2 shoulderToElbow)
        {
            var mirrored = CalculateRightElbowDirection(new Vector2(-shoulderToElbow.x, shoulderToElbow.y));
            return new Vector3(-mirrored.x, mirrored.y, mirrored.z);
        }

        /// <summary>
        /// 画像座標のスケールで右肩～右肘の差分ベクトルを受け取って、アバターの右肘の向きベクトルを計算する。戻り値は単位ベクトルになる
        /// </summary>
        /// <param name="shoulderToElbow"></param>
        /// <returns></returns>
        public static Vector3 CalculateRightElbowDirection(Vector2 shoulderToElbow)
        {
            // 実装のポイント
            // - 原則としては (shoulderToElbow.x, shoulderToElbow.y, 0) の方向に向けていく
            // - 体にヒジが食い込むような位置では何か前後にずらす
            // - 「ちょっとだけ脇が開いてる」の状態になりにくいように、真下 ~ 真横のあいだのカーブは調整する

            var angle = MathUtil.ClampedAtan2Deg(shoulderToElbow.y, shoulderToElbow.x);
            var magnitude = shoulderToElbow.magnitude;

            var adjustedAngle = angle switch
            {
                < -50f => -80f,
                < 0f => UnderAngleCurve(angle),
                < 60f => angle,
                _ => 60f,
            };

            var forwardFactor = Mathf.Clamp01(
                1f -
                (magnitude - ConvertToForwardMagnitudeMin) / (ConvertToForwardMagnitudeMax - ConvertToForwardMagnitudeMin)
                );
            var xyFactor = Mathf.Sqrt(1 - forwardFactor * forwardFactor);
            return new Vector3(
                Mathf.Cos(adjustedAngle * Mathf.Deg2Rad) * xyFactor,
                Mathf.Sin(adjustedAngle * Mathf.Deg2Rad) * xyFactor,
                forwardFactor * 1.3f
                );
        }
        
        // 補間の想定条件 (N: 肘を下げきってる状態として扱う境界値)
        // - f(-N) = -80
        // - f(0) = 0
        // - 単調増加
        // - 上に凸ではない (直線 or 下に凸)
        private static float UnderAngleCurve(float angle)
        {
            // 単に線形変換する: -N~0 を -80~0 に変換するだけなので、乗算でよい
            return angle * (80f / 50f);
        }
    }
}
