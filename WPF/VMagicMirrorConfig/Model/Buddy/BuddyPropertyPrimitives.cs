using System;

namespace Baku.VMagicMirrorConfig
{
    // NOTE: ここで定義する型はシリアライズの対象になることがある。
    // - SaveDataに対してシリアライズ/デシリアライズ
    // - Unityに送るときにシリアライズ
    // - Unity側でTransform2DやTransform3Dを操作した結果の受信でデシリアライズ

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

    /// <summary>
    /// NOTE: Unity上ではTransformのように扱うので全然readonly structではないが、WPFのデータ上はreadonly structっぽく扱ってよい。
    /// これは3Dのほうも同様
    /// </summary>
    public readonly struct BuddyTransform2D : IEquatable<BuddyTransform2D>
    {

        public BuddyTransform2D(BuddyVector2 position, BuddyVector3 rotation, float scale)
        {
            Position = position;
            Rotation = rotation;
            Scale = scale;
        }

        public BuddyVector2 Position { get; }
        // NOTE: floatに変えるかも
        public BuddyVector3 Rotation { get; }
        public float Scale { get; }

        public BuddyTransform2D WithPosition(BuddyVector2 position) => new(position, Rotation, Scale);
        public BuddyTransform2D WithRotation(BuddyVector3 rotation) => new(Position, rotation, Scale);
        public BuddyTransform2D WithScale(float scale) => new(Position, Rotation, scale);

        public bool Equals(BuddyTransform2D other)
            => Position.Equals(other.Position) && Rotation.Equals(other.Rotation) && Scale == other.Scale;

        public override bool Equals(object? obj) => obj is BuddyTransform2D other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Position, Rotation, Scale);
    }

    public readonly struct BuddyTransform3D : IEquatable<BuddyTransform3D>
    {

        public BuddyTransform3D(BuddyVector3 position, BuddyVector3 rotation, float scale, BuddyParentBone parentBone)
        {
            Position = position;
            Rotation = rotation;
            Scale = scale;
            ParentBone = parentBone;
        }

        public BuddyVector3 Position { get; }
        // NOTE: floatに変えるかも
        public BuddyVector3 Rotation { get; }
        public float Scale { get; }
        public BuddyParentBone ParentBone { get; }

        public BuddyTransform3D WithPosition(BuddyVector3 position) => new(position, Rotation, Scale, ParentBone);
        public BuddyTransform3D WithRotation(BuddyVector3 rotation) => new(Position, rotation, Scale, ParentBone);
        public BuddyTransform3D WithScale(float scale) => new(Position, Rotation, scale, ParentBone);
        public BuddyTransform3D WithAttachedBone(BuddyParentBone bone) => new BuddyTransform3D(Position, Rotation, Scale, bone);

        public bool Equals(BuddyTransform3D other) =>
            Position.Equals(other.Position) &&
            Rotation.Equals(other.Rotation) &&
            Scale == other.Scale &&
            ParentBone == other.ParentBone;

        public override bool Equals(object? obj) => obj is BuddyTransform3D other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Position, Rotation, Scale, ParentBone);
    }

    public enum BuddyParentBone
    {
        //TODO: 網羅的に、UnityのHumanBodyBonesと同じ名前で定義したい
        None = 0,
        Hips,
    }
}