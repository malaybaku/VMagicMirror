using System;
using System.IO;
using System.Text;
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

        // NOTE: RootApiじゃなくてIRootApiにする必要があるので注意
        [Preserve] public BuddyApi.IRootApi Api { get; }
    }

    public class ScriptCallerCSharp : ScriptCallerBase
    {
        private readonly ScriptEventInvokerCSharp _eventInvoker;
        private readonly BuddySettingsRepository _settings;
        private readonly BuddyAdvancedSettingsRepository _advancedSettings;
        private readonly BuddyLogger _logger;
        private readonly CancellationTokenSource _runScriptCts = new();

        private Script<object> _script;
        private ScriptState<object> _scriptState;
            
        [Inject]
        public ScriptCallerCSharp(
            string entryScriptPath,
            BuddySettingsRepository settings,
            BuddyAdvancedSettingsRepository advancedSettings,
            ApiImplementBundle apiImplementBundle,
            IFactory<RootApi, ScriptEventInvokerCSharp> scriptEventInvokerFactory
        ) : base(entryScriptPath, apiImplementBundle)
        {
            _settings = settings;
            _advancedSettings = advancedSettings;
            _logger = apiImplementBundle.Logger;
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
                _logger.Log(BuddyFolder, $"Script does not exist at: {EntryScriptPath}", BuddyLogLevel.Fatal);
                return;
            }

            try
            {
                var code = await File.ReadAllTextAsync(EntryScriptPath, cancellationToken);
                
                // NOTE: WithImportsについて
                // - WithImportsをするとスクリプトの編集体験(=インテリセンス回り)を快適にしづらいので、Importsしない。
                // - Importsを足すと破壊的変更になりうるので注意
                // - 自動Importsに関するオプションをmanifest.jsonに足すとかで将来にわたって何かのメンテは可能そう
                
                var scriptOptions = ScriptOptions.Default
                    //.WithImports("System", "VMagicMirror.Buddy")
                    .WithFilePath(EntryScriptPath)
                    .WithFileEncoding(Encoding.UTF8)
                    // 開発者モードの場合だけRuntimeError時の行数とかが言えてほしい
                    .WithEmitDebugInformation(_settings.DeveloperModeActive.CurrentValue)
                    // - #r が使える場合はフォルダ制限を検証する用のResolverが入る
                    // - #r を使っちゃダメな場合、単に一律禁止するResolver が入る
                    .WithMetadataResolver(_advancedSettings.EnableRDirective.CurrentValue    
                        ? BuddyAdvancedScriptMetadataReferenceResolver.Instance
                        : BuddyRDirectiveDisabledMetadataReferenceResolver.Instance
                        )
                    // Buddysより上のフォルダのスクリプトのロードを塞ぐ
                    .WithSourceResolver(IgnoreFileDefinedScriptSourceResolver.Instance)
                    .WithReferences(
                        typeof(object).Assembly,
                        typeof(BuddyApi.IRootApi).Assembly
                    );

                // NOTE: scriptStateはコールバックの呼び出し結果等を受けて更新されるが、VMMのコードからは直接見に行かない
                _script = CSharpScript.Create(code, scriptOptions, globalsType: typeof(CSharpScriptGlobals));

                // 使っちゃダメな想定の namespace を扱っている場合、RunAsync前に停止
                if (!_advancedSettings.SkipNamespaceLimitation.CurrentValue)
                {
                    var analyzeResult = CSharpScriptAnalyzer.AnalyzeCompileResult(_script.GetCompilation());
                    if (analyzeResult.HasError)
                    {
                        _logger.LogScriptAnalyzeError(BuddyFolder, analyzeResult.Message);
                        return;
                    }
                }
                
                _scriptState = await _script.RunAsync(
                    new CSharpScriptGlobals(Api),
                    cancellationToken: cancellationToken);

                _eventInvoker.Initialize();
            }
            catch (CompilationErrorException compilationErrorException)
            {
                _logger.LogCompileError(BuddyFolder, compilationErrorException);
            }
            catch (Exception ex)
            {
                // NOTE:
                // - エラーそのもの + 「最初のロードでコケてますよ」の2トピックがある…という扱いにしたいので2回ログを取る
                // - 非開発者に対してはエラーそのものの内容がWPF側に表示されるのが期待値
                _logger.LogRuntimeException(BuddyFolder, ex);
                _logger.Log(BuddyFolder, "Failed to load script at:" + EntryScriptPath, BuddyLogLevel.Fatal);
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