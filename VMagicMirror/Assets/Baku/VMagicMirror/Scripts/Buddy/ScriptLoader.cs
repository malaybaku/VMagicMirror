using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniRx;
using Zenject;

namespace Baku.VMagicMirror.Buddy
{
    // ScriptRoot以下の構造はこうする
    // VMM_Files/Buddy/
    // - CharaA/
    //   - main.xx
    //   - sprite.png
    //   - ...
    // - CharaB/
    //   - main.xx
    //   - sprite.png
    //   - ...
   
    public class ScriptLoader : PresenterBase
    {
        private readonly IMessageReceiver _receiver;
        private readonly IFactory<string, IScriptCaller> _scriptCallerFactory;
        private readonly List<IScriptCaller> _loadedScripts = new();

        private readonly Subject<IScriptCaller> _scriptLoading = new();
        /// <summary> スクリプトを新しくロードするときに発火する。APIを差し込んだりするのに使う </summary>
        public IObservable<IScriptCaller> ScriptLoading => _scriptLoading;

        private readonly Subject<IScriptCaller> _scriptDisposing = new();
        /// <summary> スクリプトを破棄するときに発火する。リソースの解放とか破棄に使う </summary>
        public IObservable<IScriptCaller> ScriptDisposing => _scriptDisposing;
        
        [Inject]
        public ScriptLoader(
            IMessageReceiver receiver,
            IFactory<string, IScriptCaller> scriptCallerFactory)
        {
            _receiver = receiver;
            _scriptCallerFactory = scriptCallerFactory;
        }
        
        public override void Initialize()
        {
            _receiver.AssignCommandHandler(
                VmmCommands.BuddyDisable,
                c => DisableBuddy(c.Content)
            );

            _receiver.AssignCommandHandler(
                VmmCommands.BuddyEnable,
                c => EnableBuddy(c.Content)
            );
        }

        private void DisableBuddy(string dir)
        {
            var existingScriptIndex = _loadedScripts.FindIndex(
                s => string.Compare(s.EntryScriptDirectory, dir, StringComparison.OrdinalIgnoreCase) == 0
                );
            if (existingScriptIndex < 0)
            {
                return;
            }
            
            var existingScript = _loadedScripts[existingScriptIndex];
            _loadedScripts.RemoveAt(existingScriptIndex);

            _scriptDisposing.OnNext(existingScript);
            existingScript.Dispose();
        }

        private void EnableBuddy(string dir)
        {
            // エントリポイントがなければ必ず無視
            var entryScriptPath = Path.Combine(dir, SpecialFiles.BuddyEntryScriptFileName);
            if (!File.Exists(entryScriptPath))
            {
                return;
            }

            // 読み込み済みなら無視する: リロードしたい場合、WPF側がDisable => Enableを順に送ってくるのが期待挙動
            if (_loadedScripts.Any(
                s => string.Compare(s.EntryScriptPath, entryScriptPath, StringComparison.OrdinalIgnoreCase) == 0
                ))
            {
                return;
            }

            var caller = _scriptCallerFactory.Create(entryScriptPath);
            _loadedScripts.Add(caller);
            _scriptLoading.OnNext(caller);
            caller.Initialize();
        }

        public override void Dispose()
        {
            base.Dispose();
            foreach (var script in _loadedScripts)
            {
                script.Dispose();
            }
            _loadedScripts.Clear();
        }
    }
}
