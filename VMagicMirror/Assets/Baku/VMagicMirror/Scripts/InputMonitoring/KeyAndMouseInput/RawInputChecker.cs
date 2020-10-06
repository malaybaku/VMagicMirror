using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Windows.Forms;
using UnityEngine;
using Linearstar.Windows.RawInput;
using Linearstar.Windows.RawInput.Native;
using UniRx;
using Zenject;
using Random = UnityEngine.Random;

namespace Baku.VMagicMirror
{
    /// <summary> RawInput的なマウス情報を返してくるやつ </summary>
    public class RawInputChecker : MonoBehaviour, IReleaseBeforeQuit, IKeyMouseEventSource
    {
        private const string MouseLDownEventName = "LDown";
        private const string MouseRDownEventName = "RDown";
        private const string MouseMDownEventName = "MDown";
        
        private WindowProcedureHook _windowProcedureHook = null;

        public IObservable<string> PressedRawKeys => _rawKeys;
        public IObservable<string> PressedKeys => _keys;
        public IObservable<string> MouseButton => _mouseButton;
        
        private readonly Subject<string> _rawKeys = new Subject<string>();
        private readonly Subject<string> _keys = new Subject<string>();
        private readonly Subject<string> _mouseButton = new Subject<string>();

        private bool _randomizeKey = false;

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
        
        //叩いたキーのコード。(多分大丈夫なんだけど)イベントハンドラを短時間で抜けときたいのでこういう持ち方にする
        private readonly ConcurrentQueue<int> _downKeys = new ConcurrentQueue<int>();
        
        #endregion
        
        [Inject]
        public void Initialize(IMessageReceiver receiver)
        {
            receiver.AssignCommandHandler(
                VmmCommands.EnableFpsAssumedRightHand,
                c => EnableFpsAssumedRightHand = c.ToBoolean()
                );
            receiver.AssignCommandHandler(
                VmmCommands.EnableHidRandomTyping,
                c => _randomizeKey = c.ToBoolean()
                );
            
        }
        
        private void Start()
        {
            //NOTE: このイベントはエディタ実行では飛んできません(Window Procedureに関わるので)
            _windowProcedureHook = new WindowProcedureHook();
            _windowProcedureHook.StartObserve();
            _windowProcedureHook.ReceiveRawInput += OnReceiveRawInput;
        }

        private void Update()
        {
            while (_downKeys.TryDequeue(out int keyCode))
            {
                //キーイベントとしてマウスボタンの情報も載ってくることに注意。
                //この辺のコードはWinFormKeysにも載ってるし"Virtual Key Code"とかでググると出ます
                if (keyCode == 1)
                {
                    _mouseButton.OnNext(MouseLDownEventName);
                }
                else if (keyCode == 2)
                {
                    _mouseButton.OnNext(MouseRDownEventName);
                }
                else if (keyCode == 4)
                {
                    _mouseButton.OnNext(MouseMDownEventName);
                }
                else
                {
                    var rawKey = ((Keys)keyCode).ToString();
                    _rawKeys.OnNext(rawKey);
                    
                    if (_randomizeKey)
                    {
                        var keys = RandomKeyboardKeys.RandomKeyNames;
                        _keys.OnNext(keys[Random.Range(0, keys.Length)]);
                    }
                    else
                    {
                        _keys.OnNext(rawKey);
                    }
                }
            }
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
            _downKeys.Enqueue(keyCode);
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
