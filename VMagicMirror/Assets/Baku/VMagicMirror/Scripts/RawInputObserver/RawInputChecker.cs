using System;
using System.Collections.Concurrent;
using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror
{
    //NOTE: とくに管理してないが、このクラスは複数あるとマズイです(グローバルフックが二重にかかってしまうので)
    
    /// <summary> キーボード/マウスボタンイベントを監視してUIスレッドで発火してくれる凄いやつだよ </summary>
    public class RawInputChecker : MonoBehaviour
    {
        public IObservable<string> PressedKeys => _pressedKeys;
        public IObservable<string> MouseButton => _mouseButton;
        
        private readonly Subject<string> _pressedKeys = new Subject<string>();
        private readonly Subject<string> _mouseButton = new Subject<string>();
        
        private readonly ConcurrentQueue<string> _pressedKeysConcurrent = new ConcurrentQueue<string>();
        public readonly ConcurrentQueue<string> _mouseButtonConcurrent = new ConcurrentQueue<string>();
        
        private MouseHook _mouseHook = null;
        private KeyboardHook _keyboardHook = null;
        
        private void Start()
        {
            _keyboardHook = new KeyboardHook();
            _mouseHook = new MouseHook();
            _mouseHook.SetHook();
            
            _keyboardHook.KeyboardHooked += OnKeyboardHookEvent;
            _mouseHook.MouseButton += OnMouseButtonEvent;
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
            _keyboardHook.KeyboardHooked -= OnKeyboardHookEvent;
            _mouseHook.MouseButton -= OnMouseButtonEvent;
            _mouseHook.RemoveHook();
            _mouseHook.Dispose();
            _keyboardHook.Dispose();
        }

        private void OnKeyboardHookEvent(object sender, KeyboardHookedEventArgs e)
        {
            if (e.UpDown == KeyboardUpDown.Down)
            {
                _pressedKeysConcurrent.Enqueue(e.KeyCode.ToString());
            }
        }

        private void OnMouseButtonEvent(object sender, MouseButtonEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Info))
            {
                _mouseButtonConcurrent.Enqueue(e.Info);
            }
        }
    }
}
