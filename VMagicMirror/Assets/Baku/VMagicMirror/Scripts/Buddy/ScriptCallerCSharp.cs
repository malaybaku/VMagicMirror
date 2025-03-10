using System;
using System.IO;
using System.Threading;
using Baku.VMagicMirror.Buddy.Api;
using Cysharp.Threading.Tasks;
using Zenject;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using UnityEngine.Scripting;
using BuddyApi = VMagicMirror.Buddy;

namespace Baku.VMagicMirror.Buddy
{
    public class CSharpScriptGlobals
    {
        public CSharpScriptGlobals(RootApi api)
        {
            Api = api;
        }

        [Preserve] public RootApi Api { get; }
    }

    public class ScriptCallerCSharp : ScriptCallerBase
    {
        private readonly ScriptEventInvokerCSharp _eventInvoker;
        private readonly CancellationTokenSource _runScriptCts = new();

        private Script<object> _script;
        private ScriptState<object> _scriptState;
            
        [Inject]
        public ScriptCallerCSharp(
            string entryScriptPath,
            BuddySpriteCanvas spriteCanvas,
            ApiImplementBundle apiImplementBundle,
            IFactory<RootApi, ScriptEventInvokerCSharp> scriptEventInvokerFactory
        ) : base(entryScriptPath, spriteCanvas, apiImplementBundle)
        {
            _eventInvoker = scriptEventInvokerFactory.Create(Api);
        }

        public override void Initialize()
        {
            base.Initialize();
            InitializeAndCallStartMethodAsync(_runScriptCts.Token).Forget();
        }

        private async UniTaskVoid InitializeAndCallStartMethodAsync(CancellationToken cancellationToken)
        {
            if (!File.Exists(EntryScriptPath))
            {
                BuddyLogger.Instance.Log(BuddyId, $"[Error] Script does not exist at: {EntryScriptPath}");
                return;
            }

            // TODO: まだ雑ですよ！
            try
            {
                var code = await File.ReadAllTextAsync(EntryScriptPath, cancellationToken);

                // NOTE:
                // - WithImportsをするとスクリプトの編集体験(=インテリセンス回り)を快適にするのが難しいのが既知なため、Importsしない。
                // - Importsを足すことは破壊的変更になりうる…というのは注意すること
                // - 自動Importsに関するオプションをmanifest.jsonに足すとかで回避はできるので、まあ程々に…

                var scriptOptions = ScriptOptions.Default
                    //.WithImports("System", "VMagicMirror.Buddy")
                    .WithReferences(
                        typeof(object).Assembly,
                        typeof(BuddyApi.IRootApi).Assembly
                    );

                // NOTE: scriptStateはコールバックの呼び出し結果等を受けて更新されるが、VMMのコードからは直接見に行かない
                _script = CSharpScript.Create(code, scriptOptions, globalsType: typeof(CSharpScriptGlobals));
                _scriptState = await _script.RunAsync(
                    new CSharpScriptGlobals(Api),
                    cancellationToken: cancellationToken);

                _eventInvoker.Initialize();
            }
            catch (CompilationErrorException compilationErrorException)
            {
                // NOTE: コンパイルエラーに対してはスタックトレースの表示が余計なので、ログの出し方を変える。
                // 何なら、このケースではWPF側に通知を送ったりしてもよい。
                BuddyLogger.Instance.Log(BuddyId, $"[Error] Script has compile error. {compilationErrorException.Message}");
            }
            catch (Exception ex)
            {
                //TODO: compile errorの場合の表示をスッキリさせたい
                BuddyLogger.Instance.Log(BuddyId, "[Error] Failed to load script at:" + EntryScriptPath);
                BuddyLogger.Instance.Log(BuddyId, ex);
            }
        }
        
        public override void Dispose()
        {
            base.Dispose();
            _eventInvoker.Dispose();
            _runScriptCts.Cancel();
            _runScriptCts.Dispose();
        }
    }
}