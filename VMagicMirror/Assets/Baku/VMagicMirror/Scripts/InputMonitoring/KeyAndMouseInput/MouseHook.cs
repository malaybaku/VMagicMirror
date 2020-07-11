using System;
using System.Diagnostics;

namespace Baku.VMagicMirror
{
    public class MouseHook
    {
        private IntPtr _hHook;
        //明示的に参照保持しないとデリゲートがGCされてしまうのでわざわざ参照を持つ(アンマネージ感がすごい)
        private readonly WindowsAPI.HOOKPROC _hookProc;
        
        public MouseHook()
        {
            _hookProc = HookProc;
            string moduleName = Process.GetCurrentProcess().MainModule?.ModuleName ?? "";
            IntPtr hModule = string.IsNullOrEmpty(moduleName)
                ? IntPtr.Zero
                : WindowsAPI.GetModuleHandle(moduleName);
            
            _hHook = WindowsAPI.SetWindowsHookEx(
                (int)WindowsAPI.HookType.WH_MOUSE_LL,
                _hookProc, 
                hModule,
                0
            );
        }
        
        public event Action<int> MouseButton;
        
        public void RemoveHook() => WindowsAPI.UnhookWindowsHookEx(_hHook);
        
        private IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode != WindowsAPI.HC_ACTION)
            {
                return WindowsAPI.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
            }
            
            try
            {
                int wParamVal = wParam.ToInt32();
                switch (wParamVal)
                {
                    case WindowsAPI.MouseMessages.WM_LBUTTONDOWN:
                    case WindowsAPI.MouseMessages.WM_LBUTTONUP:
                    case WindowsAPI.MouseMessages.WM_RBUTTONDOWN:
                    case WindowsAPI.MouseMessages.WM_RBUTTONUP:
                    case WindowsAPI.MouseMessages.WM_MBUTTONDOWN:
                    case WindowsAPI.MouseMessages.WM_MBUTTONUP:
                        MouseButton?.Invoke(wParamVal);
                        break;
                }
            }
            catch (Exception)
            {
                //ここはLogOutputに流さない: キーボード叩くたびにファイルI/Oは流石にまずい
            }

            return WindowsAPI.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

    }
}
