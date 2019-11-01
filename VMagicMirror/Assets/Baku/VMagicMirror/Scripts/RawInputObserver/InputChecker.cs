using System.Collections.Concurrent;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public class InputChecker : MonoBehaviour
    {
        public ConcurrentQueue<string> PressedKeys { get; } = new ConcurrentQueue<string>();
        public ConcurrentQueue<string> MouseButtonEvents { get; }= new ConcurrentQueue<string>();
        
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
                PressedKeys.Enqueue(e.KeyCode.ToString());
            }
        }

        private void OnMouseButtonEvent(object sender, MouseButtonEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Info))
            {
                MouseButtonEvents.Enqueue(e.Info);
            }
        }
    }
}
