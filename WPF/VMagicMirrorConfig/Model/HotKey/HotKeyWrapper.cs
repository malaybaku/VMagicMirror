using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// HotKeyの登録をラップするすごいやつだよ
    /// </summary>
    /// <remarks>
    /// thanks to: https://sourcechord.hatenablog.com/entry/2017/02/13/005456
    /// </remarks>
    public class HotKeyWrapper : IDisposable
    {
        private readonly IntPtr _windowHandle;
        private readonly Dictionary<int, HotKeyRegisterItem> _hotkeyList = new();

        private int _hotkeyId = 0;
        private bool _disposed = false;

        public HotKeyWrapper(Window window)
        {
            var host = new WindowInteropHelper(window);
            _windowHandle = host.Handle;
            ComponentDispatcher.ThreadPreprocessMessage += OnReceiveThreadPreprocessMessage;
        }

        ~HotKeyWrapper()
        {
            Dispose();
        }

        public event Action<HotKeyActionContent>? HotKeyActionRequested;

        public int Register(HotKeyRegisterItem item)
        {
            var modKeyNum = (uint)item.ModifierKeys | NativeApi.MOD_NOREPAT;
            var vKey = (uint)KeyInterop.VirtualKeyFromKey(item.Key);

            var ret = NativeApi.RegisterHotKey(_windowHandle, _hotkeyId, modKeyNum, vKey);
            if (ret != 0)
            {
                _hotkeyList[_hotkeyId] = item;
                var result = _hotkeyId;
                _hotkeyId++;
                return result;
            }
            else
            {
                //ここを通過する場合、キーの組み合わせが悪い(ファンクションキーと組み合わせてたりするとここに来る)
                return -1;
            }
        }

        public bool Unregister(int id)
        {
            var ret = NativeApi.UnregisterHotKey(_windowHandle, id);
            return ret == 0;
        }

        public bool UnregisterAll()
        {
            var result = true;
            foreach (var item in _hotkeyList)
            { 
                result &= Unregister(item.Key);
            }

            return result;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                UnregisterAll();
                _disposed = true;
            }
        }

        private void OnReceiveThreadPreprocessMessage(ref MSG msg, ref bool handled)
        {
            if (msg.message != NativeApi.WM_HOTKEY)
            {
                return;
            }

            var id = msg.wParam.ToInt32();
            if (_hotkeyList.TryGetValue(id, out var item))
            {
                HotKeyActionRequested?.Invoke(item.ActionContent);
            }
        }

        static class NativeApi
        {
            public const int WM_HOTKEY = 0x0312;
            public const int MAX_HOTKEY_ID = 0xC000;
            public const uint MOD_NOREPAT = 0x4000;

            [DllImport("user32.dll")]
            public static extern int RegisterHotKey(IntPtr hWnd, int id, uint modKey, uint vKey);

            [DllImport("user32.dll")]
            public static extern int UnregisterHotKey(IntPtr hWnd, int id);
        }
    }
}