using System;
using System.Runtime.InteropServices;

namespace Baku.VMagicMirrorConfig.LargePointer
{
    internal static class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(out POINT lpPoint);
        public static POINT GetWindowsMousePosition()
            => GetCursorPos(out POINT pos) ? pos : new POINT();

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong); /*x uint o int unchecked*/
        [DllImport("user32.dll")]
        public static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

        public static int GWL_STYLE => -16;
        public static uint WS_POPUP => 0x8000_0000;
        public static uint WS_VISIBLE => 0x1000_0000;
        public static int GWL_EXSTYLE => -20;
        public static uint WS_EX_TRANSPARENT => 0x0000_0020;
        public static uint WS_EX_TOOLWINDOW => 0x0000_0080;
        public static uint WS_EX_LAYERED => 0x0080_0000;
        public static uint WS_EX_NOACTIVATE => 0x0800_0000;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

        [DllImport("user32.dll")]
        private static extern IntPtr SetActiveWindow(IntPtr hWnd);

        public static void ActivateWindow(IntPtr hWnd) => SetActiveWindow(hWnd);

        public static RECT GetWindowRect(IntPtr hWnd)
            => GetWindowRect(hWnd, out RECT rect) ? rect : new RECT();

        [Flags()]
        public enum SetWindowPosFlags : uint
        {
            AsynchronousWindowPosition = 0x4000,
            DeferErase = 0x2000,
            DrawFrame = 0x0020,
            FrameChanged = 0x0020,
            HideWindow = 0x0080,
            DoNotActivate = 0x0010,
            DoNotCopyBits = 0x0100,
            IgnoreMove = 0x0002,
            DoNotChangeOwnerZOrder = 0x0200,
            DoNotRedraw = 0x0008,
            DoNotReposition = 0x0200,
            DoNotSendChangingEvent = 0x0400,
            IgnoreResize = 0x0001,
            IgnoreZOrder = 0x0004,
            ShowWindow = 0x0040,
            NoFlag = 0x0000,
            IgnoreMoveAndResize = IgnoreMove | IgnoreResize,
        }

        public static void SetWindowPosition(IntPtr hWnd, int x, int y) => SetWindowPos(
            hWnd, IntPtr.Zero, x, y, 0, 0, 
            SetWindowPosFlags.IgnoreResize | SetWindowPosFlags.DoNotActivate
            );

    }
}
