using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    //NOTE: とくに管理してないが、このクラスは複数あるとマズイです(グローバルフックが二重にかかってしまうので)
    
    /// <summary> キーボード/マウスボタンイベントを監視してUIスレッドで発火してくれる凄いやつだよ </summary>
    public class RawInputChecker : MonoBehaviour, IReleaseBeforeQuit
    {
        //NOTE: ランダム打鍵で全部のキーを叩かせる理由がない(それだと腕が動きすぎる懸念がある)ので絞っておく
        private static readonly string[] RandomKeyNames　= new []
        {
            "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", 
            "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z",
            "D1", "D2", "D3", "D4", "D5", "D6", "D7", "D8", "D9", "D0", 
        };

        private static readonly Dictionary<int, string> MouseEventNumberToEventName = new Dictionary<int, string>()
        {
            [WindowsAPI.MouseMessages.WM_LBUTTONDOWN] = "LDown",
            [WindowsAPI.MouseMessages.WM_LBUTTONUP] = "LUp",
            [WindowsAPI.MouseMessages.WM_RBUTTONDOWN] = "RDown",
            [WindowsAPI.MouseMessages.WM_RBUTTONUP] = "RUp",
            [WindowsAPI.MouseMessages.WM_MBUTTONDOWN] = "MDown",
            [WindowsAPI.MouseMessages.WM_MBUTTONUP] = "MUp",
        };
        
        public IObservable<string> PressedKeys => _pressedKeys;
        public IObservable<string> MouseButton => _mouseButton;
        
        private readonly Subject<string> _pressedKeys = new Subject<string>();
        private readonly Subject<string> _mouseButton = new Subject<string>();
        
        private readonly ConcurrentQueue<string> _pressedKeysConcurrent = new ConcurrentQueue<string>();
        private readonly ConcurrentQueue<string> _mouseButtonConcurrent = new ConcurrentQueue<string>();

        private Thread _thread;
        private bool _hasStopped = false;

        private bool _randomizeKey;

        private readonly Atomic<bool> _shouldStop = new Atomic<bool>();

        [Inject]
        public void Initialize(IMessageReceiver receiver)
        {
            receiver.AssignCommandHandler(
                VmmCommands.EnableHidRandomTyping,
                c => _randomizeKey = c.ToBoolean()
            );
        }

        public void ReleaseBeforeCloseConfig()
        {
        }

        public async Task ReleaseResources()
        {
            StopObserve();
            if (_thread != null)
            {
                await Task.Run(() => _thread.Join());
            }
        }
        
        private void StopObserve()
        {
            if (_thread == null || _hasStopped)
            {
                return;
            }
            
            _hasStopped = true;
            _shouldStop.Value = true;
            //NOTE: このメッセージは無効値をブン投げてエラーまたはメッセージループを回す(=次でwhileを抜ける)よう仕向けるもの
            WinApi.PostThreadMessage(_thread.ManagedThreadId, WinApi.WM_INPUT, IntPtr.Zero, IntPtr.Zero);
        }
        
        private void Start()
        {
            _thread = new Thread(InputObserveThread);
            _thread.Start();
        }

        private void Update()
        {
            while (_pressedKeysConcurrent.TryDequeue(out string key))
            {
                if (_randomizeKey)
                {
                    key = RandomKeyNames[UnityEngine.Random.Range(0, RandomKeyNames.Length)];
                }
                _pressedKeys.OnNext(key);
            }

            while (_mouseButtonConcurrent.TryDequeue(out string info))
            {
                _mouseButton.OnNext(info);
            }
        }

        private void OnDestroy() => StopObserve();

        private void OnKeyboardHookEvent(object sender, KeyboardHookedEventArgs e)
        {
            if (e.UpDown == KeyboardUpDown.Down)
            {
                _pressedKeysConcurrent.Enqueue(e.KeyCode.ToString());
            }
        }

        private void OnMouseButtonEvent(int wParamVal)
        {
            _mouseButtonConcurrent.Enqueue(MouseEventNumberToEventName[wParamVal]);
        }

        private void InputObserveThread()
        {
            var keyboardHook = new KeyboardHook();
            var mouseHook = new MouseHook();
            keyboardHook.KeyboardHooked += OnKeyboardHookEvent; 
            mouseHook.MouseButton += OnMouseButtonEvent;

            IntPtr msgPtr = IntPtr.Zero;
            while (!_shouldStop.Value)
            {
                int res = WinApi.GetMessage(msgPtr, IntPtr.Zero, 0, 0);
                //0: WM_QUITの受信
                //-1: エラー
                if (res == 0 || res == -1)
                {
                    break;
                }
                LogOutput.Instance.Write("recv input message");
                WinApi.TranslateMessage(msgPtr);
                WinApi.DispatchMessage(msgPtr);
            }

            keyboardHook.KeyboardHooked -= OnKeyboardHookEvent; 
            mouseHook.MouseButton -= OnMouseButtonEvent;
            keyboardHook.Dispose();
            mouseHook.RemoveHook();
        }

        static class WinApi
        {
            [DllImport("user32.dll")]
            public static extern int GetMessage(IntPtr lpMsg, IntPtr hWnd, uint filterMin, uint filterMax);
            
            [DllImport("user32.dll")]
            public static extern bool TranslateMessage(IntPtr lpMsg);

            [DllImport("user32.dll")]
            public static extern int DispatchMessage(IntPtr lpMsg);

            [DllImport("user32.dll")]
            public static extern bool PostThreadMessage(int idThread, uint msg, IntPtr wParam, IntPtr lParam);

            public const int WM_QUIT = 0x0012;
            public const int WM_INPUT = 0x00FF;
        }
    }
}
