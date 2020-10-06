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
    public class GlobalHookInputChecker : MonoBehaviour, IReleaseBeforeQuit, IKeyMouseEventSource
    {
        private static readonly Dictionary<int, string> MouseEventNumberToEventName = new Dictionary<int, string>()
        {
            [WindowsAPI.MouseMessages.WM_LBUTTONDOWN] = "LDown",
            [WindowsAPI.MouseMessages.WM_LBUTTONUP] = "LUp",
            [WindowsAPI.MouseMessages.WM_RBUTTONDOWN] = "RDown",
            [WindowsAPI.MouseMessages.WM_RBUTTONUP] = "RUp",
            [WindowsAPI.MouseMessages.WM_MBUTTONDOWN] = "MDown",
            [WindowsAPI.MouseMessages.WM_MBUTTONUP] = "MUp",
        };

        public IObservable<string> PressedRawKeys => _pressedRawKeys;
        public IObservable<string> PressedKeys => _pressedKeys;
        public IObservable<string> MouseButton => _mouseButton;
        
        private readonly Subject<string> _pressedRawKeys = new Subject<string>();
        private readonly Subject<string> _pressedKeys = new Subject<string>();
        private readonly Subject<string> _mouseButton = new Subject<string>();
        
        private readonly ConcurrentQueue<string> _pressedKeysConcurrent = new ConcurrentQueue<string>();
        private readonly ConcurrentQueue<string> _mouseButtonConcurrent = new ConcurrentQueue<string>();

        private Thread _thread;
        private bool _hasStopped = false;

        private bool _randomizeKey;

        private readonly Atomic<uint> _threadId = new Atomic<uint>();
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

        public Task ReleaseResources() => Task.CompletedTask;
        
        private void Start()
        {
            _thread = new Thread(InputObserveThread);
            _thread.Start();
        }

        private void Update()
        {
            while (_pressedKeysConcurrent.TryDequeue(out string key))
            {
                _pressedRawKeys.OnNext(key);
                if (_randomizeKey)
                {
                    var keys = RandomKeyboardKeys.RandomKeyNames;
                    key = keys[UnityEngine.Random.Range(0, keys.Length)];
                }
                _pressedKeys.OnNext(key);
            }

            while (_mouseButtonConcurrent.TryDequeue(out string info))
            {
                _mouseButton.OnNext(info);
            }
        }

        private void OnDestroy()
        {
            if (_thread == null || _hasStopped)
            {
                return;
            }
            
            _hasStopped = true;
            _shouldStop.Value = true;
            // var threadId = _threadId.Value;
            // WinApi.PostThreadMessage(threadId, WinApi.WM_APP_THREAD_QUIT, IntPtr.Zero, IntPtr.Zero);
        }

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
            _threadId.Value = WinApi.GetCurrentThreadId();
            var keyboardHook = new KeyboardHook();
            var mouseHook = new MouseHook();
            keyboardHook.KeyboardHooked += OnKeyboardHookEvent; 
            mouseHook.MouseButton += OnMouseButtonEvent;

            try
            {
                IntPtr msgPtr = IntPtr.Zero;
                WinApi.PeekMessage(msgPtr, IntPtr.Zero, 0, 0, WinApi.PM_NOREMOVE);
                while (!_shouldStop.Value)
                {
                    int res = WinApi.GetMessage(msgPtr, IntPtr.Zero, 0, 0);
                    //NOTE: res == 0, -1は普通起きない
                    if (_shouldStop.Value || res == 0 || res == -1)
                    {
                        break;
                    }
                    
                    LogOutput.Instance.Write("recv input message");
                    WinApi.TranslateMessage(msgPtr);
                    WinApi.DispatchMessage(msgPtr);
                }
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }

            keyboardHook.KeyboardHooked -= OnKeyboardHookEvent; 
            mouseHook.MouseButton -= OnMouseButtonEvent;
            keyboardHook.Dispose();
            mouseHook.RemoveHook();
        }

        static class WinApi
        {
            [DllImport("user32.dll")]
            public static extern bool PeekMessage(IntPtr lpMsg, IntPtr hWnd, uint filterMin, uint filterMax, uint wRemoveMsg);
            
            [DllImport("user32.dll")]
            public static extern int GetMessage(IntPtr lpMsg, IntPtr hWnd, uint filterMin, uint filterMax);
            
            [DllImport("user32.dll")]
            public static extern bool TranslateMessage(IntPtr lpMsg);

            [DllImport("user32.dll")]
            public static extern int DispatchMessage(IntPtr lpMsg);

            [DllImport("user32.dll")]
            public static extern bool PostThreadMessage(uint idThread, uint msg, IntPtr wParam, IntPtr lParam);
            
            [DllImport("kernel32.dll")]
            public static extern uint GetCurrentThreadId();
            
            public const int WM_APP = 0x8000;
            public const int WM_APP_THREAD_QUIT = WM_APP + 0x0001;
            public const int PM_NOREMOVE = 0x0000;

        }
    }
}
