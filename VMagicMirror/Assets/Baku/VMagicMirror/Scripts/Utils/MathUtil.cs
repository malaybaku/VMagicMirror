using UnityEngine;

namespace Baku.VMagicMirror
{
    public static class MathUtil 
    {
        /// <summary>
        /// [0, 1]の範囲の値を3次補間の曲線に乗るようなカーブに変換します。
        /// </summary>
        /// <param name="rate"></param>
        /// <returns></returns>
        private static float CubicEase(float rate) => 2 * rate * rate * (1.5f - rate);

        /// <summary>
        /// ベクトルを要素どうしで掛けた値を取得します。
        /// </summary>
        /// <param name="u"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Vector3 Mul(Vector3 u, Vector3 v) => new Vector3(u.x * v.x, u.y * v.y, u.z * v.z);

        /// <summary>
        /// 2次ベジエ曲線に沿った点を取得します。
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="control"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Vector3 CalculateBezier(Vector3 start, Vector3 end, Vector3 control, float t) 
            => (1 - t) * (1 - t) * start + 2 * (1 - t) * t * control + t * t * end;
    }
}
