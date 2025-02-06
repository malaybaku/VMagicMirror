using System;

namespace Baku.VMagicMirrorConfig
{
    // NOTE: ここで定義する型は現時点の実装ではUnityに一方的に送っているデータだが、そのうちUnityからも(自由編集の結果の取得で)受けてデシリアライズする想定

    public readonly struct BuddyVector2 : IEquatable<BuddyVector2>
    {
        public BuddyVector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float X { get; }
        public float Y { get; }

        public BuddyVector2 WithX(float x) => new(x, Y);
        public BuddyVector2 WithY(float y) => new(X, y);

        public bool Equals(BuddyVector2 other) => X == other.X && Y == other.Y;
        public override bool Equals(object? obj) => obj is BuddyVector2 other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y);
    }

    // NOTE: Quaternionの設定の実態もVector3に帰着する (Euler角での指定だけ認めるため)
    public readonly struct BuddyVector3 : IEquatable<BuddyVector3>
    {
        public BuddyVector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float X { get; }
        public float Y { get; }
        public float Z { get; }

        public BuddyVector3 WithX(float x) => new(x, Y, Z);
        public BuddyVector3 WithY(float y) => new(X, y, Z);
        public BuddyVector3 WithZ(float z) => new(X, Y, z);

        public bool Equals(BuddyVector3 other) => X == other.X && Y == other.Y && Z == other.Z;
        public override bool Equals(object? obj) => obj is BuddyVector3 other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    }
}