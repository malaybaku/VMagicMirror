using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Baku.VMagicMirrorConfig
{
    public static class WindowPositionUtil
    {
        public static WindowPosition GetThisWindowRightTopPosition()
        {
            GetWindowRect(Process.GetCurrentProcess().MainWindowHandle, out RECT rect);
            return new WindowPosition(rect.right, rect.top);
        }

        public struct WindowPosition
        {
            public WindowPosition(int x, int y)
            {
                X = x;
                Y = y;
            }
            public int X { get; }
            public int Y { get; }
        }

        public struct WindowRect
        {
            public WindowRect(int x, int y, int width, int height)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
            }
            public int X { get; }
            public int Y { get; }
            public int Width { get; }
            public int Height { get; }

        }

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

        [StructLayout(LayoutKind.Sequential)]
        struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }
    }
}
