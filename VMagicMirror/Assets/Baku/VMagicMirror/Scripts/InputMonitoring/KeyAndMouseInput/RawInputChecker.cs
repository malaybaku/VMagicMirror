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
    public class RawInputChecker : MonoBehaviour, IKeyMouseEventSource, IReleaseBeforeQuit
    {
        private const string MouseLDownEventName = "LDown";
        private const string MouseRDownEventName = "RDown";
        private const string MouseMDownEventName = "MDown";
        
        private WindowProcedureHook _windowProcedureHook = null;

        public IObservable<string> PressedRawKeys => _rawKeys;
        private readonly Subject<string> _rawKeys = new Subject<string>();
        
        public IObservable<string> PressedKeys => _keys;
        private readonly Subject<string> _keys = new Subject<string>();

        public IObservable<string> MouseButton => _mouseButton;
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
        
        //NOTE: これはウィンドウプロシージャ側のみが参照する値で、
        //WM_INPUTベースで上がった/下がったをここにポンポン入れていく
        private readonly bool[] _keyDownFlags = new bool[256];
        
        private bool _randomizeKey = false;

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
            //キーボードだけ登録する。マウスはUnityが自動でRegisterするらしく、下手に触ると危ないので触らない。
#if !UNITY_EDITOR
            try
            {
                RawInputDevice.RegisterDevice(
                    HidUsageAndPage.Keyboard,
                    RawInputDeviceFlags.InputSink | RawInputDeviceFlags.NoLegacy | RawInputDeviceFlags.AppKeys, 
                    NativeMethods.GetUnityWindowHandle()
                    );
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
#endif
            
            //NOTE: このイベントはエディタ実行では飛んできません(Window Procedureに関わるので)
            _windowProcedureHook = new WindowProcedureHook();
            _windowProcedureHook.StartObserve();
            _windowProcedureHook.ReceiveRawInput += OnReceiveRawInput;
        }

        private void Update()
        {
            while (_downKeys.TryDequeue(out int keyCode))
            {
                //キーイベントとしてマウスボタンの情報も載っているので理屈上正しくなるように割り当てる。
                //ただし実際にはこれらのコードには到達しないっぽいのを確認してます…
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
            
            if (data is RawInputMouseData mouseData && mouseData.Mouse.Flags.HasFlag(RawMouseFlags.MoveRelative))
            {
                AddDif(mouseData.Mouse.LastX, mouseData.Mouse.LastY);
            }
            else if (data is RawInputKeyboardData keyData)
            {
                var key = keyData.Keyboard;
                int code = GetKeyCode(key);
                //255は「良く分からん」的なキー情報なので弾く。
                //とくにNumLockがオフのときArrow / INS / DEL / HOME / END / PgUp / PgDnを叩くと、
                //(なぜか)255の入力と該当キー入力の2重のイベントが吹っ飛んでくるので、それを無視するのが狙い
                if (code < 0 || code > 254)
                {
                    return;
                }
                
                //NOTE: ↓はkey.Flags % 2 == 0と書くのと同じような意味
                bool isDown = !key.Flags.HasFlag(RawKeyboardFlags.Up);
                
                if (_keyDownFlags[code] == isDown)
                {
                    //キーが押しっぱなしの場合にイベントがバシバシ来るのをここで止める
                    return;
                }

                _keyDownFlags[code] = isDown;
                if (!isDown)
                {
                    return;
                }
                
                AddKeyDown(code);
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
        
        private void AddKeyDown(int keyCode) => _downKeys.Enqueue(keyCode);
        
        public void ReleaseBeforeCloseConfig() => _windowProcedureHook.StopObserve();

        public Task ReleaseResources() => Task.CompletedTask;

        private static int GetKeyCode(RawKeyboard key)
        {
            int code = key.VirutalKey;
            
            switch (code)
            {
                //Ctrl, Shift, Altが右か左か確定させる
                case (int) Keys.ControlKey:
                    return key.Flags.HasFlag(RawKeyboardFlags.RightKey)
                        ? (int) Keys.RControlKey
                        : (int) Keys.LControlKey;
                case (int) Keys.ShiftKey:
                    return key.Flags.HasFlag(RawKeyboardFlags.RightKey)
                        ? (int) Keys.RShiftKey
                        : (int) Keys.LShiftKey;
                case (int) Keys.Menu:
                    return key.Flags.HasFlag(RawKeyboardFlags.RightKey)
                        ? (int) Keys.RMenu
                        : (int) Keys.LMenu;      
                
                //NumPadキーと矢印キーとかの解釈を確定させる
                case (int) Keys.Insert:
                    return key.Flags.HasFlag(RawKeyboardFlags.LeftKey) ? code : (int) Keys.NumPad0;
                case (int) Keys.Delete:
                    return key.Flags.HasFlag(RawKeyboardFlags.LeftKey) ? code : (int) Keys.Decimal;
                case (int) Keys.Home:
                    return key.Flags.HasFlag(RawKeyboardFlags.LeftKey) ? code : (int) Keys.NumPad7;
                case (int) Keys.End:
                    return key.Flags.HasFlag(RawKeyboardFlags.LeftKey) ? code : (int) Keys.NumPad1;
                case (int) Keys.PageUp:
                    return key.Flags.HasFlag(RawKeyboardFlags.LeftKey) ? code : (int) Keys.NumPad9;
                case (int) Keys.PageDown:
                    return key.Flags.HasFlag(RawKeyboardFlags.LeftKey) ? code : (int) Keys.NumPad3;
                case (int) Keys.Left:
                    return key.Flags.HasFlag(RawKeyboardFlags.LeftKey) ? code : (int) Keys.NumPad4;
                case (int) Keys.Up:
                    return key.Flags.HasFlag(RawKeyboardFlags.LeftKey) ? code : (int) Keys.NumPad8;
                case (int) Keys.Down:
                    return key.Flags.HasFlag(RawKeyboardFlags.LeftKey) ? code : (int) Keys.NumPad2;
                case (int) Keys.Right:
                    return key.Flags.HasFlag(RawKeyboardFlags.LeftKey) ? code : (int) Keys.NumPad6;
                default:
                    return code; 
            }
        }
    }
}
