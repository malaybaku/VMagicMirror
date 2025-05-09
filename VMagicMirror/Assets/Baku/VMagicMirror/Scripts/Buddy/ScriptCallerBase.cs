using System.IO;
using Baku.VMagicMirror.Buddy.Api;
using UniRx;

namespace Baku.VMagicMirror.Buddy
{
    /// <summary>
    /// エントリポイントの指定に基づいてサブキャラのスクリプトを実行する基底クラス。
    /// このクラスはアプリ終了時以外にもスクリプトのリロード要求で破棄されることがあり、この方法で破棄される場合はロードしたリソースを解放する。
    /// </summary>
    /// <remarks>
    /// このクラスではスクリプトの言語に関知しない(継承先で言語特有の処置を行う)。
    /// </remarks>
    public abstract class ScriptCallerBase : IScriptCaller
    {
        internal RootApi Api { get; }
        private readonly CompositeDisposable _disposable = new();

        // NOTE: 呼び出し元クラスは、Initialize()が呼ばれる時点でこのパスにlua拡張子のファイルが存在することを保証している
        public string EntryScriptPath { get; }
        public string EntryScriptDirectory { get; }
        public string BuddyId { get; }
        public BuddyFolder BuddyFolder { get; }
        public bool IsDefaultBuddy => BuddyFolder.IsDefaultBuddy;
        
        public ScriptCallerBase(string entryScriptPath, ApiImplementBundle apiImplementBundle)
        {
            EntryScriptPath = entryScriptPath;
            EntryScriptDirectory = Path.GetDirectoryName(entryScriptPath);
            BuddyId = BuddyIdUtil.GetBuddyId(EntryScriptDirectory);
            BuddyFolder = BuddyFolder.Create(BuddyId);
            Api = new RootApi(EntryScriptDirectory, BuddyId, apiImplementBundle);
        }
        
        /// <summary>
        /// NOTE: このメソッドではスクリプトを実際に走らせる。
        /// スクリプトの実行過程で生成されたオブジェクトは <see cref="BuddyRuntimeObjectRepository"/> のほうで管理/破棄するので、Callerが直接処理する必要はない
        /// </summary>
        public virtual void Initialize()
        {
        }
        
        public virtual void Dispose()
        {
            Api.Dispose();
            _disposable.Dispose();
        }

        public void SetTransformsApi(ManifestTransformsApi api) => Api.Transforms = api;
    }
}
