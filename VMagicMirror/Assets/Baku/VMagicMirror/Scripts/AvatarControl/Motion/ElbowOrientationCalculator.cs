using UnityEngine;

namespace Baku.VMagicMirror
{
    public static class ElbowOrientationCalculator
    {
        // 画像上のNormalizeした長さベースで、ヒジ-肩の距離がこれより近くなると徐々に肘のBendGoalを前方に突き出す
        private const float ConvertToForwardMagnitude = 0.15f;
        
        public static Vector3 CalculateLeftElbowDirection(Vector2 shoulderToElbow)
        {
            var mirrored = CalculateRightElbowDirection(new Vector2(-shoulderToElbow.x, shoulderToElbow.y));
            return new Vector3(-mirrored.x, mirrored.y, mirrored.z);
        }
        
        public static Vector3 CalculateRightElbowDirection(Vector2 shoulderToElbow)
        {
            // 実装のポイント
            // - 原則としては (shoulderToElbow.x, shoulderToElbow.y, 0) の方向に向けていく
            // - 体にヒジが食い込むような位置では何か前後にずらす
            // - 「ちょっとだけ脇が開いてる」の状態になりにくいように、真下 ~ 真横のあいだのカーブは調整する

            var angle = Mathf.DeltaAngle(
                Mathf.Atan2(shoulderToElbow.y, shoulderToElbow.x) * Mathf.Rad2Deg, 0f);
            var magnitude = shoulderToElbow.magnitude;

            var adjustedAngle = angle switch
            {
                < -60f => -80f,
                < 0f => UnderAngleCurve(angle),
                < 60f => angle,
                _ => 60f,
            };
            
            
            var forwardFactor = Mathf.Clamp01(1f - magnitude / ConvertToForwardMagnitude);
            var xyFactor = Mathf.Sqrt(1 - forwardFactor * forwardFactor);
            return new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * xyFactor,
                Mathf.Sin(angle * Mathf.Deg2Rad) * xyFactor,
                forwardFactor
                );
        }
        
        // 補間の想定条件
        // - f(-60) = -80
        // - f(0) = 0
        // - 単調増加
        // - 上に凸ではない (直線 or 下に凸)
        private static float UnderAngleCurve(float angle)
        {
            // 単に線形変換する: -60~0 を -80~0 に変換するだけなので、乗算でよい
            return angle * (4f / 3f);
        }
    }
}
