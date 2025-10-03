using System;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public static class ElbowOrientationCalculator
    {
        // 画像上のNormalizeした長さで、ヒジ-肩の距離がこれより近くなると徐々に肘が前方に出ている扱いになる
        private const float Center = 0.15f;
        
        public static Vector3 CalculateLeftElbowDirection(Vector2 shoulderToElbow)
        {
            var mirrored = CalculateRightElbowDirection(new Vector2(-shoulderToElbow.x, shoulderToElbow.y));
            return new Vector3(-mirrored.x, mirrored.y, mirrored.z);
        }
        
        public static Vector3 CalculateRightElbowDirection(Vector2 shoulderToElbow)
        {
            // 実装のポイント
            // - おおまかには (shoulderToElbow.x, shoulderToElbow.y, 0) の方向に向けば良い
            // - 体にヒジが食い込むような位置では何か前後にずらす
            // - 「ちょっとだけ脇が開いてる」の状態になりにくいようにする

            var angle = Mathf.Atan2(shoulderToElbow.y, shoulderToElbow.x) * Mathf.Rad2Deg;

            throw new NotImplementedException();
        }
    }
}
