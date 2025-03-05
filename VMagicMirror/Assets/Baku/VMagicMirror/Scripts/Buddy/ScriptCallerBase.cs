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

        private readonly BuddySpriteCanvas _spriteCanvas;
        // NOTE: 呼び出し元クラスは、Initialize()が呼ばれる時点でこのパスにlua拡張子のファイルが存在することを保証している
        public string EntryScriptPath { get; }
        public string EntryScriptDirectory { get; }
        public string BuddyId { get; }
        
        public ScriptCallerBase(
            string entryScriptPath,
            BuddySpriteCanvas spriteCanvas,
            ApiImplementBundle apiImplementBundle
            )
        {
            EntryScriptPath = entryScriptPath;
            EntryScriptDirectory = Path.GetDirectoryName(entryScriptPath);
            BuddyId = Path.GetFileName(EntryScriptDirectory);
            _spriteCanvas = spriteCanvas;
            Api = new RootApi(EntryScriptDirectory, BuddyId, apiImplementBundle);
        }
        
        public virtual void Initialize()
        {
            // TODO: コードを完全に削除 or 生成されたインスタンスを何かのレポジトリに登録する感じの処理だけやりたい
            // (API経由でしかインスタンスにアクセスできないのも困る、みたいな話はあるので)
            // Api.SpriteCreated
            //     .Subscribe(sprite => sprite.Instance = _spriteCanvas.CreateSpriteInstance())
            //     .AddTo(_disposable);
        }
        
        public virtual void Dispose()
        {
            Api.Dispose();
            _disposable.Dispose();
        }

        public void SetTransformsApi(ManifestTransformsApi api) => Api.Transforms = api;
    }
}
