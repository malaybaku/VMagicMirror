using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// RawInput用のWindowsメッセージだけイベントとして取り出す事ができるようにウィンドウプロシージャを差し替えるやつです
    /// </summary>
    public class WindowProcedureHook : MonoBehaviour
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
        private bool _hasStopped = false;
        
        private void Start() 
        {
            // ウインドウプロシージャをフックする
            _newWndProc = WndProc;
            _newWndProcPtr = Marshal.GetFunctionPointerForDelegate(_newWndProc);
#if !UNITY_EDITOR
            _oldWndProcPtr = SetWindowLongPtr(
                NativeMethods.GetUnityWindowHandle(), GWLP_WNDPROC, _newWndProcPtr
                );
#endif
        }
        
        private void OnDisable()  => StopObserve();

        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_INPUT)
            {
                ReceiveRawInput?.Invoke(lParam);
            }
            return CallWindowProc(_oldWndProcPtr, hWnd, msg, wParam, lParam);
        }
     
        /// <summary> マウスの監視を停止します。 </summary>
        public void StopObserve()
        {
            if (_hasStopped)
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

        private const uint WM_INPUT = 0x00FF;
        private const int GWLP_WNDPROC = -4;
        
        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
   }
}
