namespace Baku.VMagicMirror
{
    /// <summary> リップシンク情報を出力してくれるやつの出力値を定義します。 </summary>
    public interface IMouthLipSyncSource
    {
        float A { get; }
        float I { get; }
        float U { get; }
        float E { get; }
        float O { get; }
    }

    /// <summary>
    /// ただのレコードで<see cref="IMouthLipSyncSource"/>を実装します。
    /// </summary>
    public class RecordLipSyncSource : IMouthLipSyncSource
    {
        public float A { get; set; }
        public float I { get; set; }
        public float U { get; set; }
        public float E { get; set; }
        public float O { get; set; }
    }
}
