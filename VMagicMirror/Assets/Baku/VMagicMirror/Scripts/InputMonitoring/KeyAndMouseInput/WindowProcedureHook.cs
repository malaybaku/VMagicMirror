using System;
using System.Runtime.InteropServices;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// RawInput用のWindowsメッセージだけイベントとして取り出す事ができるようにウィンドウプロシージャを差し替えるやつです
    /// </summary>
    public class WindowProcedureHook
    {
        delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// RawInputメッセージが来た場合にLParamを引数にして発火します。
        /// </summary>
        public event Action<IntPtr> ReceiveRawInput;
     
        private IntPtr _oldWndProcPtr;
        private IntPtr _newWndProcPtr;
        private WndProcDelegate _newWndProc;
        
        //起動時に1回だけスタートし、その後Disableまたは明示的な呼び出しによって止まる、という1回きりのオンオフを想定
        private bool _hasStarted = false;
        private bool _hasStopped = false;
        
        /// <summary> マウスの監視を開始します。 </summary>
        public void StartObserve()
        {
            // ウインドウプロシージャをフックする
            _newWndProc = WndProc;
            _newWndProcPtr = Marshal.GetFunctionPointerForDelegate(_newWndProc);
            _hasStarted = true;
#if !UNITY_EDITOR
            _oldWndProcPtr = SetWindowLongPtr(
                NativeMethods.GetUnityWindowHandle(), GWLP_WNDPROC, _newWndProcPtr
                );
#endif
        }

        /// <summary> マウスの監視を停止します。 </summary>
        public void StopObserve()
        {
            if (!_hasStarted || _hasStopped)
            {
                return;
            }
            
            // ウィンドウプロシージャを元に戻す
#if !UNITY_EDITOR            
            SetWindowLongPtr(NativeMethods.GetUnityWindowHandle(), GWLP_WNDPROC, _oldWndProcPtr);
#endif
            _oldWndProcPtr = IntPtr.Zero;
            _newWndProcPtr = IntPtr.Zero;
            _newWndProc = null;
            _hasStopped = true;
        }
        
        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_INPUT)
            {
                ReceiveRawInput?.Invoke(lParam);
            }
            else if (msg == WM_SETCURSOR)
            {
                // Unity側にカーソルの種類を制御させるとポインタ表示がうまく行かない (ウィンドウの縁付近で表示が切り替わらない)ので、UnityのWindowProcをバイパスする
                return DefWindowProc(hWnd, msg, wParam, lParam);
            }
            return CallWindowProc(_oldWndProcPtr, hWnd, msg, wParam, lParam);
        }

        private const uint WM_SETCURSOR = 0x0020;
        private const uint WM_INPUT = 0x00FF;
        private const int GWLP_WNDPROC = -4;
        
        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    }
}
