using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary> 回転差分をSO(3)扱いする用のユーティリティ </summary>
    /// <remarks>
    /// 回転補間をいい感じにするために定義してる
    /// </remarks>
    public static class So3
    {
        private const float Eps = 1e-8f;
        private const float Pi  = Mathf.PI;

        /// <summary>
        /// 回転からSO(3)相当のベクトルを取り出す
        /// NOTE: q は正規化されている前提
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public static Vector3 Log(Quaternion q)
        {
            // q = [xyz, w]、回転角 θ = 2*acos(w)
            var w = Mathf.Clamp(q.w, -1f, 1f);
            var theta = 2f * Mathf.Acos(w);

            // 小角域: sin(θ/2) ~ θ/2 なので、v ≈ 2*xyz
            var s = Mathf.Sqrt(Mathf.Max(1f - w * w, 0f));
            if (theta < 1e-6f || s < 1e-6f)
            {
                return new Vector3(q.x, q.y, q.z) * 2f;
            }

            // 一般域: v = θ * axis = θ * (xyz / sin(θ/2))
            float k = theta / s;
            return new Vector3(q.x * k, q.y * k, q.z * k);
        }

        /// <summary>
        /// SO(3)相当のベクトルをクォータニオンに戻す
        /// v = θ * axis（R^3） → q
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Quaternion Exp(Vector3 v)
        {
            var theta = v.magnitude;
            if (theta < 1e-6f)
            {
                // 小角近似: q ≈ [v/2, 1 - |v|^2/8]
                var half = v * 0.5f;
                var w = 1f - (theta * theta) / 8f;
                return Normalize(new Quaternion(half.x, half.y, half.z, w));
            }
            var u = v / theta;
            var halfTheta = 0.5f * theta;
            var s = Mathf.Sin(halfTheta);
            var c = Mathf.Cos(halfTheta);
            return new Quaternion(u.x * s, u.y * s, u.z * s, c);
        }

        public static Quaternion Normalize(Quaternion q)
        {
            var n = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            if (n < Eps)
            {
                return Quaternion.identity;
            }
            else
            {
                return q.normalized;
            }
        }
    }
}