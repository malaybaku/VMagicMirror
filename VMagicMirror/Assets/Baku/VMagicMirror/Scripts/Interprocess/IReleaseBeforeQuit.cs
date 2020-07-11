using System.Threading.Tasks;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// アプリケーションの終了前に解放したいリソースがあるようなインフラコードは、
    /// このインターフェースを実装します。
    /// </summary>
    public interface IReleaseBeforeQuit
    {
        /// <summary> リソースを解放します。メインスレッドで呼ばれます。 </summary>
        Task ReleaseResources();
    }
}
