using System;

namespace VMagicMirror.Buddy
{
    /// <summary>
    /// UnityEngineのVector3とほぼ同等のことが出来るようなデータ。
    /// </summary>
    public struct Vector3 : IEquatable<Vector3>
    {
        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public float x;
        public float y;
        public float z;

        public float magnitude => (float)Math.Sqrt(sqrMagnitude);
        public float sqrMagnitude => (float) ((double) x * x + (double) y * y + (double) z * z);
        public Vector3 normalized => Normalize(this);

        public static Vector3 Normalize(Vector3 value)
        {
            var num = Magnitude(value);
            return num > 9.999999747378752E-06 ? value / num : zero;
        }
        public void Set(float newX, float newY, float newZ)
        {
            x = newX;
            y = newY;
            z = newZ;
        }

        public void Scale(Vector3 scale)
        {
            x *= scale.x;
            y *= scale.y;
            z *= scale.z;
        }

        public void Normalize() => this = Normalize(this);

        public override int GetHashCode() => HashCode.Combine(x, y, z);
        public override bool Equals(object other) => other is Vector3 other1 && this.Equals(other1);
        public bool Equals(Vector3 other) => 
            (double) this.x == (double) other.x &&
            (double) this.y == (double) other.y && 
            (double) this.z == (double) other.z;

        // NOTE: 最低限それっぽくするのが目的のため、Format文字列とかは受け取らない
        public override string ToString() => $"({x:0.00}, {y:0.00}, {z:0.00})";

        public static float Distance(Vector3 a, Vector3 b)
        {
            var num1 = a.x - b.x;
            var num2 = a.y - b.y;
            var num3 = a.z - b.z;
            return (float) Math.Sqrt((double) num1 * num1 + (double) num2 * num2 + (double) num3 * num3);
        }

        public static float Magnitude(Vector3 vector) => vector.magnitude;
        public static float SqrMagnitude(Vector3 vector) => vector.sqrMagnitude;

        public static float Dot(Vector3 lhs, Vector3 rhs) 
            => (float) ((double) lhs.x * rhs.x + (double) lhs.y * rhs.y + (double) lhs.z * rhs.z);

        public static Vector3 Scale(Vector3 a, Vector3 b) => new(a.x * b.x, a.y * b.y, a.z * b.z);

        public static Vector3 Lerp(Vector3 a, Vector3 b, float t)
        {
            t = t < 0 ? 0 : t > 1 ? 1 : t;
            return new Vector3(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);
        }

        public static Vector3 LerpUnclamped(Vector3 a, Vector3 b, float t) 
            => new(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);

        public static Vector3 Cross(Vector3 lhs, Vector3 rhs)
        {
            return new Vector3(
                (float) ((double) lhs.y * rhs.z - (double) lhs.z * rhs.y),
                (float) ((double) lhs.z * rhs.x - (double) lhs.x * rhs.z), 
                (float) ((double) lhs.x * rhs.y - (double) lhs.y * rhs.x)
                );
        }

        public static Vector3 zero => new(0f, 0f, 0f);
        public static Vector3 one => new(1, 1, 1);
        public static Vector3 forward => new Vector3(0, 0, 1);
        public static Vector3 back => new Vector3(0, 0, -1);
        public static Vector3 left => new(-1, 0, 0);
        public static Vector3 up => new(0, 1, 0);
        public static Vector3 down => new(0, -1, 0);
        public static Vector3 right => new(1, 0, 0);
        
        public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator -(Vector3 a) => new(-a.x, -a.y, -a.z);
        public static Vector3 operator *(Vector3 a, float d) => new(a.x * d, a.y * d, a.z * d);
        public static Vector3 operator *(float d, Vector3 a) => new(a.x * d, a.y * d, a.z * d);
        public static Vector3 operator /(Vector3 a, float d) => new(a.x / d, a.y / d, a.z / d);
        public static bool operator ==(Vector3 lhs, Vector3 rhs)
        {
            var num1 = lhs.x - rhs.x;
            var num2 = lhs.y - rhs.y;
            var num3 = lhs.z - rhs.z;
            return (double) num1 * num1 + (double) num2 * num2 + (double) num3 * num3 < 9.999999439624929E-11;
        }
        public static bool operator !=(Vector3 lhs, Vector3 rhs) => !(lhs == rhs);
    }
}

