using System;
using IQ = UnityEngine.Quaternion;
using IV3 = UnityEngine.Vector3;

// TODO: 本当はUnityEngine.Quaternionに依存しないような実装にしたい。
// が、Quaternionの再実装はそこそこ大変なので、サボって内部的にUnityEngine.Quaternionを参照している。
// ここが自前実装になれば Api.Interface のアセンブリが UnityEngine を完全に参照しなくなって構造上良いので、なるべくやりたい
// (※完全な自前実装は不要で、System.Numericsとかを持ってくるのもアリ)
namespace Baku.VMagicMirror.Buddy.Api.Interface
{
    /// <summary>
    /// UnityEngineのQuaternionとほぼ同等のことが出来るようなデータ。
    /// </summary>
    public struct Quaternion : IEquatable<Quaternion>
    {
        public Quaternion(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public float x;
        public float y;
        public float z;
        public float w;

        public void Set(float newX, float newY, float newZ, float newW)
        {
            x = newX;
            y = newY;
            z = newZ;
            w = newW;
        }
        
        public Vector3 eulerAngles
        {
            get => ToVector3(ToIQ().eulerAngles);
            set => this = ToQuaternion(IQ.Euler(ToIV3(value)));
        }

        public Quaternion normalized => ToQuaternion(ToIQ().normalized);

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

        private IQ ToIQ() => new(x, y, z, w);
        
        private static IV3 ToIV3(Vector3 v) => new(v.x, v.y, v.z);
        private static Vector3 ToVector3(IV3 v) => new(v.x, v.y, v.z);
        private static Quaternion ToQuaternion(IQ iq) => new(iq.x, iq.y, iq.z, iq.w);

        public static float Dot(Quaternion a, Quaternion b) => IQ.Dot(a.ToIQ(), b.ToIQ());

        public static Quaternion Inverse(Quaternion rotation) => ToQuaternion(IQ.Inverse(rotation.ToIQ()));

        public static Quaternion FromToRotation(Vector3 fromDirection, Vector3 toDirection)
            => ToQuaternion(IQ.FromToRotation(ToIV3(fromDirection), ToIV3(toDirection)));
        
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t)
            => ToQuaternion(IQ.Slerp(a.ToIQ(), b.ToIQ(), t));
        
        public static Quaternion SlerpUnclamped(Quaternion a, Quaternion b, float t)
            => ToQuaternion(IQ.SlerpUnclamped(a.ToIQ(), b.ToIQ(), t));

        public static Quaternion AngleAxis(float angle, Vector3 axis)
            => ToQuaternion(IQ.AngleAxis(angle, ToIV3(axis)));

        public static Quaternion LookRotation(Vector3 forward, Vector3 upwards)
            => ToQuaternion(IQ.LookRotation(ToIV3(forward), ToIV3(upwards)));

        public static Quaternion LookRotation(Vector3 forward)
            => LookRotation(forward, Vector3.up);
        
        public static float Angle(Quaternion a, Quaternion b) 
            => IQ.Angle(a.ToIQ(), b.ToIQ());

        public static Quaternion operator *(Quaternion lhs, Quaternion rhs) 
            => ToQuaternion(lhs.ToIQ() * rhs.ToIQ());
        public static Vector3 operator *(Quaternion rotation, Vector3 point) 
            => ToVector3(rotation.ToIQ() * ToIV3(point));
        public static bool operator ==(Quaternion lhs, Quaternion rhs) => lhs.ToIQ() == rhs.ToIQ();
        public static bool operator !=(Quaternion lhs, Quaternion rhs) => !(lhs == rhs);
        
        public static Quaternion Euler(float x, float y, float z) => ToQuaternion(IQ.Euler(x, y, z));
        public static Quaternion Euler(Vector3 euler) => ToQuaternion(IQ.Euler(euler.x, euler.y, euler.z));

        public static Quaternion identity => new(0, 0, 0, 1);
    }
}

