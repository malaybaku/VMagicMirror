using System;
using System.IO;

namespace Baku.VMagicMirror.Buddy
{
    public interface IScriptLoader
    {
        /// <summary> スクリプトを新しくロードするときに発火する。APIを差し込んだりするのに使う </summary>
        IObservable<IScriptCaller> ScriptLoading { get; }

        /// <summary> スクリプトを破棄するときに発火する。リソースの解放とか破棄に使う </summary>
        IObservable<IScriptCaller> ScriptDisposing { get; }
    }

    public interface IScriptCallerPathGenerator
    {
        string CreateEntryScriptPath(string dir);
    }

    // NOTE: ファイル分けるほどでも…というサイズなので実装も同じファイルで定義してしまっている
    public class LuaScriptCallerPathGenerator : IScriptCallerPathGenerator
    {
        public string CreateEntryScriptPath(string dir) => Path.Combine(dir, "main.lua");
    }

    public class CSharpScriptCallerPathGenerator : IScriptCallerPathGenerator
    {
        public string CreateEntryScriptPath(string dir) => Path.Combine(dir, "main.cs");
    }
}
