using R3;

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

        public Observable<string> RawKeyDown => _rawInput.RawKeyDown;
        public Observable<string> RawKeyUp => _rawInput.RawKeyUp;
        public Observable<string> KeyDown => _rawInput.KeyDown;
        public Observable<string> KeyUp => _rawInput.KeyUp;
        public Observable<string> MouseButton => _globalHookInput.MouseButton;
    }
}
