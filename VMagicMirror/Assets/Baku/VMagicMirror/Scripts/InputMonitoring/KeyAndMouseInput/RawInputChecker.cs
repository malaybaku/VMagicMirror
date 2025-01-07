﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        
        // キー押下後にKey Upイベントが一定時間飛んでこない場合にキーを離したものとみなすタイムアウト値。
        // VMMのメニューバーを掴みながらKeyの上げ下げをするなどの操作をするとKey Upイベントが検出できなくなるので、その対策として用いる
        private const float KeyUpForceDuration = 2.0f;
        // キー上げ処理を検証する周期。毎フレームやるほどではないので…
        private const float KeyUpForceCheckInterval = 0.4f;
        
        private WindowProcedureHook _windowProcedureHook = null;

        public IObservable<string> RawKeyDown => _rawKeyDown;
        private readonly Subject<string> _rawKeyDown = new();
        
        public IObservable<string> RawKeyUp => _rawKeyUp;
        private readonly Subject<string> _rawKeyUp = new();
        
        public IObservable<string> KeyDown => _keyDown;
        private readonly Subject<string> _keyDown = new();
        public IObservable<string> KeyUp => _keyUp;
        private readonly Subject<string> _keyUp = new();

        public IObservable<string> MouseButton => _mouseButton;
        private readonly Subject<string> _mouseButton = new();

        private readonly object _timeLock = new();
        private float _time = 0f;
        //NOTE: Time.timeにメインスレッド以外からアクセスするためのラッパー
        private float CurrentTime
        {
            get { lock (_timeLock) return _time; }
            set { lock (_timeLock) _time = value; }
        }
        

        #region マウス
        
        private int _dx;
        private int _dy;
        private readonly object _diffLock = new();
        
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
        
        private readonly object _keyDownLock = new();

        //NOTE: 下記はおもにウィンドウプロシージャで更新しつつ、自動キー離し処理のためにメインスレッドからも見に来る。
        //基本的には WM_INPUT ベースでの現在値が入る
        private readonly bool[] _keyDownFlags = new bool[256];
        private readonly float[] _keyDownTimes = new float[256];
        
        private bool _randomizeKey = false;

        //直前フレームで下がった/上がったキーのコード。(多分大丈夫なんだけど)イベントハンドラを短時間で抜けときたいのでこういう持ち方にする
        private readonly ConcurrentQueue<int> _downKeys = new();
        private readonly ConcurrentQueue<int> _upKeys = new();
        
        //打鍵ランダム化の際、押したキーがランダムになっちゃってもKeyUpで破綻が起きなくなるようにするため、どこを押したか覚えるキュー
        private readonly Queue<string> _randomizedDownKeyQueue = new();
        
        #endregion
        
        [Inject]
        public void Initialize(IMessageReceiver receiver, IMessageSender sender)
        {
            receiver.AssignCommandHandler(
                VmmCommands.EnableFpsAssumedRightHand,
                c => EnableFpsAssumedRightHand = c.ToBoolean()
                );
            receiver.AssignCommandHandler(
                VmmCommands.EnableHidRandomTyping,
                c =>
                {
                    _randomizeKey = c.ToBoolean();
                    //値が変わる時点で残ってるキュー(普通ないけど)を消しておく
                    _randomizedDownKeyQueue.Clear();
                });
            
            //NOTE: ここ2つは「VRoid SDKを使ってる間はキーボード監視について余計な操作をやめろ」的な意味
            receiver.AssignCommandHandler(
                VmmCommands.OpenVRoidSdkUi,
                _ => UnregisterKeyboard()
                );

            sender.SendingMessage += message =>
            {
                if (message.Command == nameof(MessageFactory.VRoidModelLoadCanceled) ||
                    message.Command == nameof(MessageFactory.VRoidModelLoadCompleted))
                {
                    RegisterKeyboard();
                }
            };
        }
        
        private void Start()
        {
#if UNITY_EDITOR
            EditorSetupTargetKeyCodes();
#endif

            //キーボードだけ登録する。マウスはUnityが自動でRegisterするらしく、下手に触ると危ないので触らない。
            RegisterKeyboard();
            
            //NOTE: このイベントはエディタ実行では飛んできません(Window Procedureに関わるので)
            _windowProcedureHook = new WindowProcedureHook();
            _windowProcedureHook.StartObserve();
            _windowProcedureHook.ReceiveRawInput += OnReceiveRawInput;

            //KeyUpの検出漏れがないか一定周期で見る
            Observable.Interval(TimeSpan.FromSeconds(KeyUpForceCheckInterval))
                .Subscribe(_ => ReleaseKeyByTimeout())
                .AddTo(this);
        }

        private void Update()
        {
            CurrentTime = Time.time;
#if UNITY_EDITOR
            EditorCheckKeyDown();
#endif
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
                    _rawKeyDown.OnNext(rawKey);
                    
                    if (_randomizeKey)
                    {
                        var keys = RandomKeyboardKeys.RandomKeyNames;
                        var randomizedKey = keys[Random.Range(0, keys.Length)];
                        _randomizedDownKeyQueue.Enqueue(randomizedKey);
                        _keyDown.OnNext(randomizedKey);
                    }
                    else
                    {
                        _keyDown.OnNext(rawKey);
                    }
                }
            }
            
            while (_upKeys.TryDequeue(out var keyCode))
            {
                var rawKey = ((Keys)keyCode).ToString();
                _rawKeyUp.OnNext(rawKey);
                if (_randomizeKey)
                {
                    //ランダム化されている場合、ともかく押したキーを順に離す、という挙動にして破綻しづらくする
                    if (_randomizedDownKeyQueue.Count > 0)
                    {
                        var key = _randomizedDownKeyQueue.Dequeue();
                        _keyUp.OnNext(key);
                    }
                }
                else
                {
                    _keyUp.OnNext(rawKey);
                }
            }
        }

        private void OnDisable()
        {
            _windowProcedureHook.StopObserve();
        }

        private void OnReceiveRawInput(IntPtr lParam)
        {
            var messageData = RawInputData.FromHandle(lParam);
            ProcessRawInputData(messageData);

            //NOTE: きわめて短時間に複数のイベントが飛んできている場合、それがBufferedDataに入っているので拾いに行く
            var bufferedData = RawInputData.GetBufferedData();
            foreach (var data in bufferedData)
            {
                ProcessRawInputData(data);
            }
        }

        private void ProcessRawInputData(RawInputData data)
        {
            if (data is RawInputMouseData mouseData && mouseData.Mouse.Flags.HasFlag(RawMouseFlags.MoveRelative))
            {
                AddDif(mouseData.Mouse.LastX, mouseData.Mouse.LastY);
            }
            else if (data is RawInputKeyboardData keyData)
            {
                var key = keyData.Keyboard;
                var code = GetKeyCode(key);
                //255は「良く分からん」的なキー情報なので弾く。
                //とくにNumLockがオフのときArrow / INS / DEL / HOME / END / PgUp / PgDnを叩くと、
                //(なぜか)255の入力と該当キー入力の2重のイベントが吹っ飛んでくるので、それを無視するのが狙い
                if (code < 0 || code > 254)
                {
                    return;
                }
                
                //NOTE: ↓はkey.Flags % 2 == 0と書くのと同じような意味
                var isDown = !key.Flags.HasFlag(RawKeyboardFlags.Up);
                lock (_keyDownLock)
                {
                    if (isDown)
                    {
                        //押しっぱなし状態のとき、ここを繰り返し通過する
                        _keyDownTimes[code] = CurrentTime;
                    }
  
                    if (_keyDownFlags[code] == isDown)
                    {
                        // 特にキーが押しっぱなしの場合のイベント発火をここでガードしておく
                        return;
                    }

                    _keyDownFlags[code] = isDown;
                    if (isDown)
                    {
                        _downKeys.Enqueue(code);
                    }
                    else
                    {
                        _upKeys.Enqueue(code);
                    }
                }
            }
        }

        // キー押下したままの判定になっていてキー上げが発火していないキーを強制的に離した扱いにする
        private void ReleaseKeyByTimeout()
        {
            var t = Time.time;
            lock (_keyDownLock)
            {
                for (var i = 0; i < _keyDownFlags.Length; i++)
                {
                    // modifier系のキーは押しっぱなしでも連打相当のイベントが飛んでこないため、離した判定にするのもやめておく
                    if (IsModifierKey((Keys)i))
                    {
                        continue;
                    }

                    if (_keyDownFlags[i] && t - _keyDownTimes[i] > KeyUpForceDuration)
                    {
                        _keyDownFlags[i] = false;
                        _upKeys.Enqueue(i);
                    }
                }
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

        public void ReleaseBeforeCloseConfig() => _windowProcedureHook.StopObserve();

        public Task ReleaseResources() => Task.CompletedTask;

        private void RegisterKeyboard()
        {
            if (Application.isEditor)
            {
                return;
            }
            
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
        }

        private void UnregisterKeyboard()
        {
            if (!Application.isEditor)
            {
                RawInputDevice.UnregisterDevice(HidUsageAndPage.Keyboard);            
            }
        }
        
        private static int GetKeyCode(RawKeyboard key)
        {
            var code = key.VirutalKey;
            
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
        
        private static bool IsModifierKey(Keys key)
        {
            return key is 
                Keys.LShiftKey or Keys.RShiftKey or 
                Keys.LControlKey or Keys.RControlKey or 
                Keys.LMenu or Keys.RMenu;
        }
        
        //NOTE: DIのほうがキレイだけど、分けるほどコードサイズが無いので直書きで。
        #if UNITY_EDITOR

        private KeyCode[] _editorCheckTargetKeyCodes = null;
        private KeyCode[] _numberKeyCodes = null;

        private void EditorSetupTargetKeyCodes()
        {
            //97から"A"が始まってるのを前提に、そこから"Z"まで拾おうという狙いのコード
            var codes = new KeyCode[26];
            for (var i = 0; i < codes.Length; i++)
            {
                codes[i] = (KeyCode) (97 + i);
            }
            _editorCheckTargetKeyCodes = codes;

            _numberKeyCodes = new KeyCode[10];
            for (var i = 0; i < 10; i++)
            {
                _numberKeyCodes[i] = KeyCode.Alpha0 + i;
            }
        }
        
        private void EditorCheckKeyDown()
        {
            foreach (var keyCode in _editorCheckTargetKeyCodes)
            {
                if (Input.GetKeyDown(keyCode))
                {
                    string keyName = keyCode.ToString();
                    _rawKeyDown.OnNext(keyName);

                    if (_randomizeKey)
                    {
                        int index = Random.Range(0, _editorCheckTargetKeyCodes.Length);
                        keyName = _editorCheckTargetKeyCodes[index].ToString();
                    }
                    _keyDown.OnNext(keyName);
                }
                
                if (Input.GetKeyUp(keyCode))
                {
                    string keyName = keyCode.ToString();
                    _rawKeyUp.OnNext(keyName);
                    _keyUp.OnNext(keyName);
                }
            }
            
            foreach (var keyCode in _numberKeyCodes)
            {
                if (Input.GetKeyDown(keyCode))
                {
                    //"D0" から "D9"までのいずれか
                    var keyName = "D" + (keyCode - KeyCode.Alpha0);
                    _rawKeyDown.OnNext(keyName);

                    //randomizeの場合はA-Zに変換しちゃう(めんどいので)
                    if (_randomizeKey)
                    {
                        int index = Random.Range(0, _editorCheckTargetKeyCodes.Length);
                        keyName = _editorCheckTargetKeyCodes[index].ToString();
                    }
                    _keyDown.OnNext(keyName);
                }
                
                if (Input.GetKeyUp(keyCode))
                {
                    var keyName = "D" + (keyCode - KeyCode.Alpha0);
                    _rawKeyUp.OnNext(keyName);
                    _keyUp.OnNext(keyName);
                }
            }

            {
                var enterKey = nameof(Keys.Enter);
                
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    _keyDown.OnNext(enterKey);
                    _rawKeyDown.OnNext(enterKey);
                }

                if (Input.GetKeyUp(KeyCode.Return))
                {
                    _keyUp.OnNext(enterKey);
                    _rawKeyUp.OnNext(enterKey);
                }
            }
        }
        
        #endif
    }
}
