using System;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// RawInputのキーボードとグローバルフックのマウスを組み合わせて入力ソースにする実装。
    /// 両者の長所と短所
    /// </summary>
    public class HybridInputChecker : IKeyMouseEventSource
    {
        public HybridInputChecker(RawInputChecker rawInputChecker, GlobalHookInputChecker globalHookInputChecker)
        {
            _rawInput = rawInputChecker;
            _globalHookInput = globalHookInputChecker;
        }

        private readonly RawInputChecker _rawInput;
        private readonly GlobalHookInputChecker _globalHookInput;

        public IObservable<string> PressedRawKeys => _rawInput.PressedRawKeys;
        public IObservable<string> PressedKeys => _rawInput.PressedKeys;
        public IObservable<string> MouseButton => _globalHookInput.MouseButton;
    }
}
