using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// 何かのアクションを実行したあとメッセージループで待機できるスレッド
    /// </summary>
    public class MessageLoopThread
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private Action _actStart = () => { };
        private Action _actEnd = () => { };

        private Thread? _thread = null;

        private readonly object _threadIdLock = new object();
        private uint _threadId = 0;
        private uint ThreadId
        {
            get { lock (_threadIdLock) return _threadId; }
            set { lock (_threadIdLock) _threadId = value; }
        }

        public void Run(Action actStart, Action actEnd)
        {
            if (_thread != null)
            {
                return;
            }

            _actStart = actStart;
            _actEnd = actEnd;
            
            _thread = new Thread(() => ThreadWithMessageLoop(_cts.Token));
            _thread.Start();
        }

        public void Stop()
        {
            _cts.Cancel();
            WinApi.PostThreadMessage(ThreadId, WinApi.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
            _thread = null;
            ThreadId = 0;
        }

        private void ThreadWithMessageLoop(CancellationToken token)
        {
            _actStart();
            ThreadId = WinApi.GetCurrentThreadId();

            //NOTE: msgPtrにちゃんと領域確保しておかないとGetMessageがキレるので注意
            var msgPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WinApi.MSG)));
            try
            {
                WinApi.PeekMessage(msgPtr, IntPtr.Zero, 0, 0, WinApi.PM_NOREMOVE);
                while (!token.IsCancellationRequested)
                {
                    int res = WinApi.GetMessage(msgPtr, IntPtr.Zero, 0, 0);
                    //NOTE: res == 0, -1は普通起きない
                    if (token.IsCancellationRequested || res == 0 || res == -1)
                    {
                        break;
                    }

                    //普通ここは通らないはず
                    LogOutput.Instance.Write("recv input message");
                    WinApi.TranslateMessage(msgPtr);
                    WinApi.DispatchMessage(msgPtr);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogOutput.Instance.Write(ex);
            }
            finally
            {
                Marshal.FreeHGlobal(msgPtr);
                msgPtr = IntPtr.Zero;
            }

            ThreadId = 0;
            _actEnd();
        }


        static class WinApi
        {
            [DllImport("user32.dll")]
            public static extern bool PeekMessage(IntPtr lpMsg, IntPtr hWnd, uint filterMin, uint filterMax, uint wRemoveMsg);

            [DllImport("user32.dll")]
            public static extern int GetMessage(IntPtr lpMsg, IntPtr hWnd, uint filterMin, uint filterMax);

            [DllImport("user32.dll")]
            public static extern bool TranslateMessage(IntPtr lpMsg);

            [DllImport("user32.dll")]
            public static extern int DispatchMessage(IntPtr lpMsg);

            [DllImport("kernel32.dll")]
            public static extern uint GetCurrentThreadId();

            [DllImport("user32.dll")]
            public static extern bool PostThreadMessage(uint threadId, uint msg, IntPtr wParam, IntPtr lParam);

            public const int PM_NOREMOVE = 0x0000;
            public const int WM_QUIT = 0x0012;

            [StructLayout(LayoutKind.Sequential)]
            public struct MSG
            {
                public IntPtr hwnd;
                public int message;
                public IntPtr wParam;
                public IntPtr lParam;
                public uint time;
                public int pt;
            }
        }
    }
}
