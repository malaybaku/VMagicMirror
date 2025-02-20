using System;
using System.IO;
using System.Threading;
using Baku.VMagicMirror.Buddy.Api;
using Cysharp.Threading.Tasks;
using Zenject;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using UnityEngine;
using UnityEngine.Scripting;

namespace Baku.VMagicMirror.Buddy
{
    public class CSharpScriptGlobals
    {
        public CSharpScriptGlobals(RootApi api)
        {
            Api = api;
        }

        [Preserve] 
        public RootApi Api { get; }
    }

    /// <summary>
    /// <see cref="ScriptCaller"/>をCSharpスクリプトで乗っ取るすごいやつだよ
    /// </summary>
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

        public override string CreateEntryScriptPath(string dir) => Path.Combine(dir, "main.cs");

        public override void Initialize()
        {
            base.Initialize();
            InitializeAndCallStartMethodAsync(_runScriptCts.Token).Forget();
        }

        private async UniTaskVoid InitializeAndCallStartMethodAsync(CancellationToken cancellationToken)
        {
            if (!File.Exists(EntryScriptPath))
            {
                LogOutput.Instance.Write($"Error, script does not exist at: {EntryScriptPath}");
                return;
            }

            // TODO: まだ雑ですよ！
            try
            {
                var code = await File.ReadAllTextAsync(EntryScriptPath, cancellationToken);

                // TODO: asm渡しすぎてそう、ちょっと絞りたい
                // TODO: 逆にImportsは少なすぎの予感ある
                var scriptOptions = ScriptOptions.Default
                    .WithImports("System", "UnityEngine")
                    .WithReferences(
                        typeof(object).Assembly,
                        typeof(Transform).Assembly,
                        typeof(RootApi).Assembly
                    );

                // NOTE:
                //   Scriptは一回だけ実行する (コールバック登録とかまでそれで終わらす)
                //   …というモデルだが、内部の変数とか更新するうえで不都合だったら直すかも。
                _script = CSharpScript.Create(code, scriptOptions, globalsType: typeof(CSharpScriptGlobals));
                _scriptState = await _script.RunAsync(
                    new CSharpScriptGlobals(Api),
                    cancellationToken: cancellationToken);
                
                _eventInvoker.Initialize();
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write("Failed to load script at:" + EntryScriptPath);
                LogOutput.Instance.Write(ex);
                Debug.LogException(ex);
            }
        }
        
        public override void Dispose()
        {
            base.Dispose();
            _runScriptCts.Cancel();
            _runScriptCts.Dispose();
        }
    }
}