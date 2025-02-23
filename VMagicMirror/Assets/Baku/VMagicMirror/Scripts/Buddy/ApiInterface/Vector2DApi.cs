using System;

namespace Baku.VMagicMirror.Buddy.Api.Interface
{
    /// <summary>
    /// UnityEngineのVector2とほぼ同等のことが出来るようなデータ。
    /// </summary>
    public struct Vector2 : IEquatable<Vector2>
    {
        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public float x;
        public float y;

        public float magnitude => (float)Math.Sqrt(sqrMagnitude);
        public float sqrMagnitude => (float)((double)x * x + (double)y * y);

        public Vector2 normalized
        {
            get
            {
                var result = new Vector2(x, y);
                result.Normalize();
                return result;
            }
        }

        public void Set(float newX, float newY)
        {
            x = newX;
            y = newY;
        }
        
        public void Scale(Vector2 scale)
        {
            x *= scale.x;
            y *= scale.y;
        }

        public void Normalize()
        {
            float magnitude = this.magnitude;
            if ((double) magnitude > 9.999999747378752E-06)
                this = this / magnitude;
            else
                this = Vector2.zero;
        }

        public float SqrMagnitude() => SqrMagnitude(this);

        public override int GetHashCode() => HashCode.Combine(x, y);
        public override bool Equals(object other)
            => other is Vector2 other1 && this.Equals(other1);
        public bool Equals(Vector2 other) 
            => (double) this.x == (double) other.x && (double) this.y == (double) other.y;

        public static float Dot(Vector2 lhs, Vector2 rhs)
        {
            return (float) ((double) lhs.x * rhs.x + (double) lhs.y * rhs.y);
        }

        public static Vector2 Lerp(Vector2 a, Vector2 b, float t)
        {
            t = t < 0 ? 0 : t > 1 ? 1 : t;
            return new Vector2(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t);
        }

        public static Vector2 LerpUnclamped(Vector2 a, Vector2 b, float t) 
            => new(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t);

        public static Vector2 Scale(Vector2 a, Vector2 b) 
            => new(a.x * b.x, a.y * b.y);

        public static float Distance(Vector2 a, Vector2 b)
        {
            var num1 = a.x - b.x;
            var num2 = a.y - b.y;
            return (float) Math.Sqrt((double) num1 * num1 + (double) num2 * num2);
        }

        public static float SqrMagnitude(Vector2 a)
        {
            return (float) ((double) a.x * a.x + (double) a.y * a.y);
        }


        // TODO: operator含めて色々と定義する

        public static Vector2 zero => new(0f, 0f);
        public static Vector2 one => new(1f, 1f);
        public static Vector2 up => new(0.0f, 1f);
        public static Vector2 down => new(0.0f, -1f);
        public static Vector2 left => new(-1f, 0.0f);
        public static Vector2 right => new(1f, 0.0f);
        
        public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.x + b.x, a.y + b.y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.x - b.x, a.y - b.y);
        public static Vector2 operator *(Vector2 a, Vector2 b) => new(a.x * b.x, a.y * b.y);
        public static Vector2 operator /(Vector2 a, Vector2 b) => new(a.x / b.x, a.y / b.y);
        public static Vector2 operator -(Vector2 a) => new(-a.x, -a.y);
        public static Vector2 operator *(Vector2 a, float d) => new(a.x * d, a.y * d);
        public static Vector2 operator *(float d, Vector2 a) => new(a.x * d, a.y * d);
        public static Vector2 operator /(Vector2 a, float d) => new(a.x / d, a.y / d);
        public static bool operator ==(Vector2 lhs, Vector2 rhs)
        {
            var num1 = lhs.x - rhs.x;
            var num2 = lhs.y - rhs.y;
            return (double) num1 * num1 + (double) num2 * num2 < 9.999999439624929E-11;
        }
        public static bool operator !=(Vector2 lhs, Vector2 rhs) => !(lhs == rhs);
        public static implicit operator Vector2(Vector3 v) => new(v.x, v.y);
        public static implicit operator Vector3(Vector2 v) => new(v.x, v.y, 0.0f);
    }
}

