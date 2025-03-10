using System;

// TODO: 本当はUnityEngine.Quaternionに依存しないような実装にしたい。
// が、Quaternionの再実装はそこそこ大変なので、サボって内部的にUnityEngine.Quaternionを参照している。
// ここが自前実装になれば Api.Interface のアセンブリが UnityEngine を完全に参照しなくなって構造上良いので、なるべくやりたい
// (※完全な自前実装は不要で、System.Numericsとかを持ってくるのもアリ)
namespace VMagicMirror.Buddy
{
    /// <summary>
    /// UnityEngineのQuaternionとほぼ同等のことが出来る四元数のデータです。
    /// 回転を表すことを目的として定義されています。
    /// </summary>
    public partial struct Quaternion : IEquatable<Quaternion>
    {
        /// <summary>
        /// 各成分の値を指定してインスタンスを初期化します。
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="w"></param>
        public Quaternion(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        /// <summary> x成分を取得、設定します。 </summary>
        public float x;
        /// <summary> y成分を取得、設定します。 </summary>
        public float y;
        /// <summary> z成分を取得、設定します。 </summary>
        public float z;
        /// <summary> w成分を取得、設定します。 </summary>
        public float w;

        /// <summary>
        /// 成分の値を指定してインスタンスを更新します。
        /// </summary>
        /// <param name="newX"></param>
        /// <param name="newY"></param>
        /// <param name="newZ"></param>
        /// <param name="newW"></param>
        public void Set(float newX, float newY, float newZ, float newW)
        {
            x = newX;
            y = newY;
            z = newZ;
            w = newW;
        }
        
        /// <summary>
        /// オイラー角として回転を取得、設定します。
        /// </summary>
        /// <remarks>
        /// <para>
        ///   回転の適用順はUnityEngine.Quaternionに準じており、YXZの順に適用されます。
        /// </para>
        /// </remarks>
        public Vector3 eulerAngles
        {
            get => ToVector3(ToIQ().eulerAngles);
            set => this = ToQuaternion(UnityEngine.Quaternion.Euler(ToIV3(value)));
        }

        /// <summary>
        /// 大きさを正規化した値を取得します。
        /// </summary>
        public Quaternion normalized => ToQuaternion(ToIQ().normalized);

        /// <summary>
        /// 回転をdegreeで表した角度と回転軸に分解した値を取得します。
        /// </summary>
        /// <param name="angle">度数法で表した回転角度</param>
        /// <param name="axis">回転軸の単位ベクトル</param>
        public void ToAngleAxis(out float angle, out Vector3 axis)
        {
            ToIQ().ToAngleAxis(out angle, out var rawAxis);
            axis = ToVector3(rawAxis);
        }

        public override int GetHashCode() => HashCode.Combine(x, y, z, w);
        public override bool Equals(object other) => other is Quaternion other1 && Equals(other1);
        public bool Equals(Quaternion other)
        {
            return this.x.Equals(other.x) && this.y.Equals(other.y) && this.z.Equals(other.z) && this.w.Equals(other.w);
        }

        /// <summary>
        /// Quaternionをベクトルとして見たときの内積を計算します。
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>内積の値</returns>
        public static float Dot(Quaternion a, Quaternion b) => UnityEngine.Quaternion.Dot(a.ToIQ(), b.ToIQ());

        /// <summary>
        /// 指定した回転の逆回転を取得します。
        /// </summary>
        /// <param name="rotation">計算元となる回転</param>
        /// <returns>逆回転として計算された回転</returns>
        public static Quaternion Inverse(Quaternion rotation) => ToQuaternion(UnityEngine.Quaternion.Inverse(rotation.ToIQ()));

        /// <summary>
        /// ある方向から別の方向に向くような回転を取得します。
        /// </summary>
        /// <param name="fromDirection"></param>
        /// <param name="toDirection"></param>
        /// <returns><paramref name="fromDirection"/>から<paramref name="toDirection"/>に向くような回転</returns>
        public static Quaternion FromToRotation(Vector3 fromDirection, Vector3 toDirection)
            => ToQuaternion(UnityEngine.Quaternion.FromToRotation(ToIV3(fromDirection), ToIV3(toDirection)));

        /// <summary>
        /// 2つの回転を球面補間した値を取得します。
        /// </summary>
        /// <param name="a"><paramref name="t"/>が0のときに適用される回転</param>
        /// <param name="b"><paramref name="t"/>が1のときに適用される回転</param>
        /// <param name="t">補間の適用率を表す値。0であれば結果が<paramref name="a"/>となり、1であれば結果は<paramref name="b"/>に一致します。</param>
        /// <returns>補間された回転</returns>
        /// <remarks>
        ///   <paramref name="t"/>の値は0以上1以下に丸めて適用されます。
        /// </remarks>
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t)
            => ToQuaternion(UnityEngine.Quaternion.Slerp(a.ToIQ(), b.ToIQ(), t));
        
        /// <summary>
        /// 2つの回転を球面補間した値を取得します。
        /// <see cref="Slerp"/>と異なり、<paramref name="t"/>の値は [0, 1] の範囲に丸められません。
        /// </summary>
        /// <param name="a"><paramref name="t"/>が0のときに適用される回転</param>
        /// <param name="b"><paramref name="t"/>が1のときに適用される回転</param>
        /// <param name="t">補間の適用率を表す値。0であれば結果が<paramref name="a"/>となり、1であれば結果は<paramref name="b"/>に一致します。</param>
        /// <returns>補間された回転</returns>
        public static Quaternion SlerpUnclamped(Quaternion a, Quaternion b, float t)
            => ToQuaternion(UnityEngine.Quaternion.SlerpUnclamped(a.ToIQ(), b.ToIQ(), t));

        /// <summary>
        /// 度数法で表した回転角および回転軸ベクトルを指定することで、回転を表す値を生成します。
        /// </summary>
        /// <param name="angle">度数法で表した回転角</param>
        /// <param name="axis">回転軸ベクトル</param>
        /// <returns>指定した角度および軸に基づいた回転</returns>
        public static Quaternion AngleAxis(float angle, Vector3 axis)
            => ToQuaternion(UnityEngine.Quaternion.AngleAxis(angle, ToIV3(axis)));

        /// <summary>
        /// ワールド座標上で指定した方向を向き、かつローカル座標上で上方向が指定した向きになるような回転を生成します。
        /// </summary>
        /// <param name="forward">正面を表すワールド座標上の方向</param>
        /// <param name="upwards">カメラの上方向を表すようなワールド座標上の方向</param>
        /// <returns>指定した方向を向くような回転</returns>
        /// <remarks>
        ///   <paramref name="upwards"/>は省略可能であり、省略した場合は<see cref="Vector3.up"/>が指定されたものとして扱われます。
        /// </remarks>
        public static Quaternion LookRotation(Vector3 forward, Vector3 upwards)
            => ToQuaternion(UnityEngine.Quaternion.LookRotation(ToIV3(forward), ToIV3(upwards)));

        /// <summary>
        /// ワールド座標上で指定した方向を向くような回転を生成します。
        /// </summary>
        /// <param name="forward">正面を表すワールド座標上の方向</param>
        /// <returns>指定した方向を向くような回転</returns>
        /// <remarks>
        ///   この関数では、視点の上方向は<see cref="Vector3.up"/>であるものとして計算が行われます。
        /// </remarks>
        public static Quaternion LookRotation(Vector3 forward)
            => LookRotation(forward, Vector3.up);
        
        /// <summary>
        /// 指定した回転どうしのなす角度を取得します。
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>指定した回転どうしのなす角度</returns>
        public static float Angle(Quaternion a, Quaternion b) 
            => UnityEngine.Quaternion.Angle(a.ToIQ(), b.ToIQ());

        /// <summary>
        /// 回転を合成します。
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns>合成した回転</returns>
        public static Quaternion operator *(Quaternion lhs, Quaternion rhs) 
            => ToQuaternion(lhs.ToIQ() * rhs.ToIQ());

        /// <summary>
        /// 回転をベクトルに適用します。
        /// </summary>
        /// <param name="rotation">ベクトルに適用したい回転</param>
        /// <param name="point">回転させる対象となるベクトル</param>
        /// <returns>回転を適用した状態のベクトル</returns>
        public static Vector3 operator *(Quaternion rotation, Vector3 point) 
            => ToVector3(rotation.ToIQ() * ToIV3(point));
        public static bool operator ==(Quaternion lhs, Quaternion rhs) => lhs.ToIQ() == rhs.ToIQ();
        public static bool operator !=(Quaternion lhs, Quaternion rhs) => !(lhs == rhs);
        
        /// <summary>
        /// オイラー角を指定して回転を生成します。
        /// </summary>
        /// <param name="x">x軸まわりの回転角を度数法で表した値</param>
        /// <param name="y">y軸まわりの回転角を度数法で表した値</param>
        /// <param name="z">z軸まわりの回転角を度数法で表した値</param>
        /// <returns>指定したオイラー角に基づく回転</returns>
        /// <remarks>
        ///   回転の適用順はUnityEngine.Quaternionに準じており、YXZの順に適用されます。
        /// </remarks>
        public static Quaternion Euler(float x, float y, float z) => ToQuaternion(UnityEngine.Quaternion.Euler(x, y, z));
        /// <summary>
        /// オイラー角を指定して回転を生成します。
        /// </summary>
        /// <param name="euler">xyzの各軸まわりの回転角を度数法で表した値</param>
        /// <returns>指定したオイラー角に基づく回転</returns>
        /// <remarks>
        ///   回転の適用順はUnityEngine.Quaternionに準じており、YXZの順に適用されます。
        /// </remarks>
        public static Quaternion Euler(Vector3 euler) => ToQuaternion(UnityEngine.Quaternion.Euler(euler.x, euler.y, euler.z));

        /// <summary>
        /// 回転を行わないことを表す値を取得します。
        /// </summary>
        public static Quaternion identity => new(0, 0, 0, 1);
    }
}

