using System;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// MainViewerシーンの実行時点でロード対象のモデルが判別出来ている場合にそのデータを保持するインターフェース。
    /// </summary>
    /// <remarks>
    /// アプリケーションにデフォルトモデル等を定義できる場合のI/Fとして検証も兼ねて追加しています。
    /// </remarks>
    public interface IVRMPreloadData
    {
        public bool HasData { get; }

        /// <summary> VRMのバイナリです。 </summary>
        public byte[] GetData();
    }

    /// <summary>
    /// プリロードするデータが特にないようなVRMPreloadDataの実装。
    /// </summary>
    /// <remarks>
    /// 特にIVRMPreloadDataを使わないときの実体としてコレを使う
    /// </remarks>
    public class EmptyVRMPreloadData : IVRMPreloadData
    {
        public bool HasData => false;
        public byte[] GetData() => Array.Empty<byte>();
    }
}
