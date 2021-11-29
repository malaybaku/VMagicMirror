using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// マウス入力をグローバルフックするのに使えるやつ。1インスタンスで1回だけフックを行う
    /// </summary>
    internal class MouseHook : IDisposable
    {
        public MouseHook()
        {
            _hookProc = HookProc;
        }

        private IntPtr _hHook;
        //デリゲートがGCされるのを避けるために、明示的に参照を持つ
        private readonly MouseHookWinAPI.HOOKPROC _hookProc;

        public event Action<int>? MouseButton;

        public void Start()
        {
            string moduleName = Process.GetCurrentProcess().MainModule?.ModuleName ?? "";
            IntPtr hModule = string.IsNullOrEmpty(moduleName)
                ? IntPtr.Zero
                : MouseHookWinAPI.GetModuleHandle(moduleName);

            _hHook = MouseHookWinAPI.SetWindowsHookEx(
                (int)MouseHookWinAPI.HookType.WH_MOUSE_LL,
                _hookProc,
                hModule,
                0
            );
        }

        public void Dispose()
        {
            MouseHookWinAPI.UnhookWindowsHookEx(_hHook);
        }

        private IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode != MouseHookWinAPI.HC_ACTION)
            {
                return MouseHookWinAPI.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
            }

            try
            {
                int wParamVal = wParam.ToInt32();
                switch (wParamVal)
                {
                    case MouseMessages.WM_LBUTTONDOWN:
                    case MouseMessages.WM_LBUTTONUP:
                    case MouseMessages.WM_RBUTTONDOWN:
                    case MouseMessages.WM_RBUTTONUP:
                    case MouseMessages.WM_MBUTTONDOWN:
                    case MouseMessages.WM_MBUTTONUP:
                        MouseButton?.Invoke(wParamVal);
                        break;
                }
            }
            catch (Exception)
            {
                //ここはLogOutputに流さない: キーボード叩くたびにファイルI/Oは流石にまずい
            }

            return MouseHookWinAPI.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        private static class MouseHookWinAPI
        {
            [DllImport("user32.dll")]
            public static extern IntPtr SetWindowsHookEx(int idHook, HOOKPROC lpfn, IntPtr hMod, int dwThreadId);

            public delegate IntPtr HOOKPROC(int nCode, IntPtr wParam, IntPtr lParam);

            [DllImport("user32.dll")]
            public static extern bool UnhookWindowsHookEx(IntPtr hHook);

            [DllImport("user32.dll")]
            public static extern IntPtr CallNextHookEx(IntPtr hHook, int nCode, IntPtr wParam, IntPtr lParam);

            [DllImport("kernel32.dll", EntryPoint = "GetModuleHandleW", SetLastError = true)]
            public static extern IntPtr GetModuleHandle(string moduleName);

            public const int HC_ACTION = 0;

            public enum HookType : int
            {
                WH_MSGFILTER = -1,
                WH_JOURNALRECORD = 0,
                WH_JOURNALPLAYBACK = 1,
                WH_KEYBOARD = 2,
                WH_GETMESSAGE = 3,
                WH_CALLWNDPROC = 4,
                WH_CBT = 5,
                WH_SYSMSGFILTER = 6,
                WH_MOUSE = 7,
                WH_HARDWARE = 8,
                WH_DEBUG = 9,
                WH_SHELL = 10,
                WH_FOREGROUNDIDLE = 11,
                WH_CALLWNDPROCRET = 12,
                WH_KEYBOARD_LL = 13,
                WH_MOUSE_LL = 14,
            }
        }
    }

    public static class MouseMessages
    {
        public const int WM_LBUTTONDOWN = 0x0201;
        public const int WM_LBUTTONUP = 0x0202;

        public const int WM_RBUTTONDOWN = 0x0204;
        public const int WM_RBUTTONUP = 0x0205;

        public const int WM_MBUTTONDOWN = 0x0207;
        public const int WM_MBUTTONUP = 0x0208;
    }
}
