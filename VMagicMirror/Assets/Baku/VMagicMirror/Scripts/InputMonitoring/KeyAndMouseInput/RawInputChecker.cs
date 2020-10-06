using System;
using System.Threading.Tasks;
using UnityEngine;
using Linearstar.Windows.RawInput;
using Linearstar.Windows.RawInput.Native;
using UniRx;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary> RawInput的なマウス情報を返してくるやつ </summary>
    public class RawInputChecker : MonoBehaviour, IReleaseBeforeQuit, IKeyMouseEventSource
    {
        private WindowProcedureHook _windowProcedureHook = null;

        public IObservable<string> PressedRawKeys => _rawKeys;
        public IObservable<string> PressedKeys => _keys;
        public IObservable<string> MouseButton => _mouseButton;s
        
        private readonly Subject<string> _rawKeys = new Subject<string>();
        private readonly Subject<string> _keys = new Subject<string>();
        //NOTE: コレだけはとりあえず発火させない: RawInputとの相性が悪そうなため
        private readonly Subject<string> _mouseButton = new Subject<string>();

        #region マウス
        
        private int _dx;
        private int _dy;
        private readonly object _diffLock = new object();
        
        public bool EnableFpsAssumedRightHand { get; private set; } = false;

        /// <summary>
        /// 前回呼び出してからの間にマウス移動が積分された合計値を取得します。
        /// 読み出すことで累計値は0にリセットされます。
        /// </summary>
        /// <returns></returns>
        public (int dx, int dy) GetAndReset()
        {
            lock (_diffLock)
            {
                int x = _dx;
                int y = _dy;
                _dx = 0;
                _dy = 0;
                return (x, y);
            }
        }

        #endregion
        
        #region キーボード
        
        
        #endregion
        
        [Inject]
        public void Initialize(IMessageReceiver receiver)
        {
            receiver.AssignCommandHandler(
                VmmCommands.EnableFpsAssumedRightHand,
                c => EnableFpsAssumedRightHand = c.ToBoolean()
                );
        }
        
        private void Start()
        {
            //NOTE: このイベントはエディタ実行では飛んできません(Window Procedureに関わるので)
            _windowProcedureHook = new WindowProcedureHook();
            _windowProcedureHook.StartObserve();
            _windowProcedureHook.ReceiveRawInput += OnReceiveRawInput;
        }

        private void OnDisable()
        {
            _windowProcedureHook.StopObserve();
        }

        private void OnReceiveRawInput(IntPtr lParam)
        {
            var data = RawInputData.FromHandle(lParam);
            
            if (data is RawInputMouseData mouseData &&
                mouseData.Mouse.Flags.HasFlag(RawMouseFlags.MoveRelative))
            {
                AddDif(mouseData.Mouse.LastX, mouseData.Mouse.LastY);
            }
            else if (data is RawInputKeyboardData keyData &&
                     keyData.Keyboard.Flags.HasFlag(RawKeyboardFlags.Down))
            {
                AddKeyDown(keyData.Keyboard.VirutalKey, keyData.Keyboard.Flags.HasFlag(RawKeyboardFlags.RightKey));
            }
        }

        private void AddDif(int dx, int dy)
        {
            lock (_diffLock)
            {
                _dx += dx;
                _dy += dy;                
            }
        }
        
        private void AddKeyDown(int keyCode, bool isRightKey)
        {
            
            
        }

        public void ReleaseBeforeCloseConfig()
        {
            _windowProcedureHook.StopObserve();
        }

        public Task ReleaseResources()
        {
            return Task.CompletedTask;
        }

    }
}
