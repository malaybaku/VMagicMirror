using System;
using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// VMagicMirrorにおいて、IK動作のデータソースとして利用可能なインターフェースを定義します。
    /// </summary>
    /// <remarks>
    /// NOTE: 1つのコンポーネントが2つ以上のIIKDataを持っててもいいことに注意
    /// </remarks>
    public interface IIKData
    {
        /// <summary>ワールド位置を取得します。</summary>
        Vector3 Position { get; }

        /// <summary>ワールド回転を取得します。</summary>
        Quaternion Rotation { get; }
    }

    /// <summary>単純なレコード的な形式による<see cref="IIKData"/>の実装です。</summary>
    public class IKDataRecord : IIKData
    {
        public Vector3 Position { get; set; } = Vector3.zero;

        public Quaternion Rotation { get; set; } = Quaternion.identity;

        //public IKTargets Target { get; set; } = IKTargets.Body;
    }

    public readonly struct IKDataStruct : IIKData, IEquatable<IKDataStruct>
    {
        public IKDataStruct(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }

        public bool Equals(IKDataStruct other) 
            => Position.Equals(other.Position) && Rotation.Equals(other.Rotation);

        public override bool Equals(object obj) 
            => obj is IKDataStruct other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (Position.GetHashCode() * 397) ^ Rotation.GetHashCode();
            }
        }

        public static IKDataStruct Empty { get; } = new IKDataStruct(Vector3.zero, Quaternion.identity);
    }

    /// <summary><see cref="IIKData"/>のIKが体のどの部位を動かすためのものかの一覧</summary>
    public enum IKTargets
    {
        Body,
        LHand,
        RHand,
        HeadLookAt,
    }
}
