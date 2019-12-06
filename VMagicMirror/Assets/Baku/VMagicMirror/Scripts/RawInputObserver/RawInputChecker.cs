using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror
{
    //NOTE: とくに管理してないが、このクラスは複数あるとマズイです(グローバルフックが二重にかかってしまうので)
    
    /// <summary> キーボード/マウスボタンイベントを監視してUIスレッドで発火してくれる凄いやつだよ </summary>
    public class RawInputChecker : MonoBehaviour
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
        
        public IObservable<string> PressedKeys => _pressedKeys;
        public IObservable<string> MouseButton => _mouseButton;
        
        private readonly Subject<string> _pressedKeys = new Subject<string>();
        private readonly Subject<string> _mouseButton = new Subject<string>();
        
        private readonly ConcurrentQueue<string> _pressedKeysConcurrent = new ConcurrentQueue<string>();
        private readonly ConcurrentQueue<string> _mouseButtonConcurrent = new ConcurrentQueue<string>();

        private Thread _thread;
        
        private void Start()
        {
            _thread = new Thread(InputObserveThread);
            _thread.Start();
        }

        private void Update()
        {
            while (_pressedKeysConcurrent.TryDequeue(out string key))
            {
                _pressedKeys.OnNext(key);
            }

            while (_mouseButtonConcurrent.TryDequeue(out string info))
            {
                _mouseButton.OnNext(info);
            }
        }

        private void OnDestroy()
        {
            int threadId = _thread.ManagedThreadId;
            WinApi.PostThreadMessage(threadId, WinApi.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
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
            var keyboardHook = new KeyboardHook();
            var mouseHook = new MouseHook();
            keyboardHook.KeyboardHooked += OnKeyboardHookEvent; 
            mouseHook.MouseButton += OnMouseButtonEvent;

            IntPtr msgPtr = IntPtr.Zero;
            while (WinApi.GetMessage(msgPtr, IntPtr.Zero, 0, 0))
            {
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
            public static extern bool GetMessage(IntPtr lpMsg, IntPtr hWnd, uint filterMin, uint filterMax);
            
            [DllImport("user32.dll")]
            public static extern bool TranslateMessage(IntPtr lpMsg);

            [DllImport("user32.dll")]
            public static extern int DispatchMessage(IntPtr lpMsg);

            [DllImport("user32.dll")]
            public static extern bool PostThreadMessage(int idThread, uint msg, IntPtr wParam, IntPtr lParam);

            public const int WM_QUIT = 0x0012;
        }
    }
}
