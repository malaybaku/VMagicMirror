using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Forms = System.Windows.Forms;
using UnityEngine;
using Linearstar.Windows.RawInput;
using Linearstar.Windows.RawInput.Native;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// RawInput方式でマウス移動イベントを取得するすごいやつだよ
    /// </summary>
    public class RawMouseMoveChecker : MonoBehaviour
    {
        private Thread _thread = null;
        private int _dx;
        private int _dy;
        private readonly object _difLock = new object();

        /// <summary>
        /// 前回呼び出してからの間にマウス移動が積分された合計値を取得します。
        /// 読み出すことで累計値は0にリセットされます。
        /// </summary>
        /// <returns></returns>
        public (int dx, int dy) GetAndReset()
        {
            lock (_difLock)
            {
                int x = _dx;
                int y = _dy;
                _dx = 0;
                _dy = 0;
                return (x, y);
            }
        }
        
        private void Start()
        {
            _thread = new Thread(ObserveRoutine);
            _thread.Start();
        }
        
        private void OnDestroy()
        {
            PostThreadMessage(_thread.ManagedThreadId, WM_QUIT, IntPtr.Zero, IntPtr.Zero);
        }

        private void AddDif(int dx, int dy)
        {
            lock (_difLock)
            {
                _dx += dx;
                _dy += dy;
            }
        }
        
        private void ObserveRoutine()
        {
            var window = new ReceiverWindow();
            window.Input += data =>
            {
                if (data is RawInputMouseData mouseData &&
                    mouseData.Mouse.Flags.HasFlag(RawMouseFlags.MoveRelative))
                {
                    AddDif(
                        mouseData.Mouse.LastX,
                        mouseData.Mouse.LastY
                    );
                }
            };

            try
            {
                RawInputDevice.RegisterDevice(
                    HidUsageAndPage.Mouse, 
                    RawInputDeviceFlags.InputSink | RawInputDeviceFlags.NoLegacy, 
                    window.Handle
                    );
                Forms.Application.Run();
            }
            finally
            {
                RawInputDevice.UnregisterDevice(HidUsageAndPage.Mouse);
            }
        }
        
        [DllImport("user32.dll")]
        static extern bool PostThreadMessage(int idThread, uint msg, IntPtr wParam, IntPtr lParam);

        const int WM_QUIT = 0x0012;
    }

    /// <summary>
    /// RawInputのイベントを受け取るために立てるWindow
    /// </summary>
    public class ReceiverWindow : NativeWindow
    {
        private const int WM_INPUT = 0x00FF;
        
        public event Action<RawInputData> Input;

        public ReceiverWindow()
        {
            CreateHandle(new CreateParams
            {
                X = 0,
                Y = 0,
                Width = 0,
                Height = 0,
                Style = 0x800000,
            });
        }

        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            if (m.Msg == WM_INPUT)
            {
                Input?.Invoke(RawInputData.FromHandle(m.LParam));
            }
            base.WndProc(ref m);
        }        
    }
    
}
