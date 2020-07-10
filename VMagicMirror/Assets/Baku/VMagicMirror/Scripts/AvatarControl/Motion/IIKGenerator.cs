using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// VMagicMirrornにおいて、IK動作のデータソースとして利用可能なインターフェースを定義します。
    /// </summary>
    /// <remarks>
    /// NOTE: 1つのコンポーネントが2つ以上の
    /// </remarks>
    public interface IIKGenerator
    {
        /// <summary>ワールド位置を取得します。</summary>
        Vector3 Position { get; }

        /// <summary>ワールド回転を取得します。</summary>
        Quaternion Rotation { get; }

        /// <summary>適用先になるIKターゲット</summary>
        IKTargets Target { get; }
    }

    /// <summary>単純なレコード的な形式による<see cref="IIKGenerator"/>の実装です。</summary>
    public class IKDataRecord : IIKGenerator
    {
        public Vector3 Position { get; set; } = Vector3.zero;

        public Quaternion Rotation { get; set; } = Quaternion.identity;

        public IKTargets Target { get; set; } = IKTargets.Body;
    }

    /// <summary><see cref="IIKGenerator"/>のIKが体のどの部位を動かすためのものかの一覧</summary>
    public enum IKTargets
    {
        Body,
        LHand,
        RHand,
        HeadLookAt,
    }
}
