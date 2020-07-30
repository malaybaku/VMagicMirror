using System.Threading.Tasks;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// アプリケーションの終了前に解放したいリソースがあるようなインフラコードは、
    /// このインターフェースを実装します。
    /// </summary>
    public interface IReleaseBeforeQuit
    {
        /// <summary> リソース解放のうち、時間があまりかからず、コンフィグ画面の閉じを保証する前にやった方がいい処理を行います。 </summary>
        void ReleaseBeforeCloseConfig();
        
        /// <summary> リソースを解放します。メインスレッドで呼ばれます。 </summary>
        Task ReleaseResources();
    }
}
