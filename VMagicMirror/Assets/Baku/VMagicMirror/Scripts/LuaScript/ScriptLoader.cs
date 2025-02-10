using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniRx;
using Zenject;

namespace Baku.VMagicMirror.LuaScript
{
    // ScriptRoot以下の構造はこうする
    // VMM_Files/SubCharacters/
    // - CharaA/
    //   - main.lua
    //   - sprite.png
    //   - ...
    // - CharaB/
    //   - main.lua
    //   - sprite.png
    //   - ...
   
    public class ScriptLoader : PresenterBase, ITickable
    {
        private readonly IMessageReceiver _receiver;
        private readonly IFactory<string, ScriptCaller> _scriptCallerFactory;
        private readonly List<ScriptCaller> _loadedScripts = new();

        private readonly Subject<ScriptCaller> _scriptLoading = new();
        /// <summary> スクリプトを新しくロードするときに発火する。APIを差し込んだりするのに使う </summary>
        public IObservable<ScriptCaller> ScriptLoading => _scriptLoading;

        private readonly Subject<ScriptCaller> _scriptDisposing = new();
        /// <summary> スクリプトを破棄するときに発火する。リソースの解放とか破棄に使う </summary>
        public IObservable<ScriptCaller> ScriptDisposing => _scriptDisposing;
        
        [Inject]
        public ScriptLoader(
            IMessageReceiver receiver,
            IFactory<string, ScriptCaller> scriptCallerFactory)
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
            // // TODO: 「初回に勝手に起動する」という挙動は後で変える
            // // - Configから明示的に指示を受けてロード開始 / リロードする感じにしたいので
            // var directories = Directory.GetDirectories(SpecialFiles.BuddyRootDirectory);
            // foreach (var dir in directories)
            // {
            //     ReloadScriptAtDir(dir);
            // }
        }

        public ScriptCaller FindScriptCaller(string buddyId)
            => _loadedScripts.FirstOrDefault(s => s.BuddyId == buddyId);
        
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
            var entryScriptPath = Path.Combine(dir, "main.lua");
            if (!File.Exists(entryScriptPath))
            {
                return;
            }

            // 読み込み済みなら無視する: リロードしたいならWPF側がDisable => Enableを順にやって欲しい…という仕様
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
        
        private void ReloadScriptAtDir(string dir)
        {
            var entryScriptPath = Path.Combine(dir, "main.lua");
            if (File.Exists(entryScriptPath))
            {
                ReloadScriptAtPath(entryScriptPath);
            }
        }
        
        // 存在する .lua ファイルをエントリポイントとして指定して呼び出す。そのパスにあるスクリプトをリロード実行する。
        // ロード済みのスクリプトの再実行、および最初の起動で呼び出す
        private void ReloadScriptAtPath(string path)
        {
            var existingScriptIndex = _loadedScripts.FindIndex(
                s => string.Compare(s.EntryScriptPath, path, StringComparison.OrdinalIgnoreCase) == 0
                );
            if (existingScriptIndex >= 0)
            {
                var existingScript = _loadedScripts[existingScriptIndex];
                _loadedScripts.RemoveAt(existingScriptIndex);
                existingScript.Dispose();
            }

            var caller = _scriptCallerFactory.Create(path);
            _loadedScripts.Add(caller);
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

        void ITickable.Tick()
        {
            foreach (var script in _loadedScripts)
            {
                script.Tick();
            }
        }
    }
}
