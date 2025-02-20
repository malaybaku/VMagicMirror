using System;
using System.IO;
using Baku.VMagicMirror.Buddy.Api;
using NLua;
using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror.Buddy
{
    // フォルダを指定されてスクリプトを実行するためのクラス。
    // このクラスは勝手に動き出さずに、ScriptLoaderからの指定に基づいて動作を開始/終了する。
    // このクラスはアプリ終了時以外にもスクリプトのリロード要求で破棄されることがあり、この方法で破棄される場合はロードしたリソースを解放する。
    public abstract class ScriptCallerBase : IScriptCaller
    {
        internal RootApi Api { get; }
        private readonly CompositeDisposable _disposable = new();

        //「定義されてれば呼ぶ」系のコールバックメソッド。今のとこupdateしかないが他も増やしてよい + 増えたら管理方法を考えた方が良さそう
        private LuaFunction _updateFunction;

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
        
        public abstract string CreateEntryScriptPath(string dir);

        public virtual void Initialize()
        {
            // APIの生成時にSpriteのインスタンスまで入ってる状態にしておく (ヒエラルキーの構築時に最初からインスタンスがあるほうが都合がよいため)
            Api.SpriteCreated
                .Subscribe(sprite => sprite.Instance = _spriteCanvas.CreateSpriteInstance())
                .AddTo(_disposable);
        }
        
        public virtual void Dispose()
        {
            Api.Dispose();
            _disposable.Dispose();
        }

        public void SetTransformsApi(TransformsApi api) => Api.Transforms = api;
    }
}
