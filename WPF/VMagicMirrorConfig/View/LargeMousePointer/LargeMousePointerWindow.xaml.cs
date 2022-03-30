using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Baku.VMagicMirrorConfig.View
{
    using static NativeMethods;

    //NOTE: ウィンドウハンドルをゴリゴリ操作する + コレ以外のコードほぼないのでコードビハインド使う。
    public partial class LargeMousePointerWindow : Window
    {
        private const int MouseTrackIntervalMillisec = 8;
        private const int MouseStopDisappearTimeMillisec = 6000;
        private const double OpacityChangeLerpFactor = 0.2;

        private const int MouseStopScaleResetTimeMillisec = 3000;
        //Sqrを使っているのは移動値を2乗値で管理しているため
        private const int ScaleIncreaseMoveDistanceSqr = 10000;
        private const double ScaleChangeLerpFactor = 0.1;
        private const double ScaleWhenStop = 0.5;
        private const double ScaleWhenMoving = 1.0;

        public LargeMousePointerWindow()
        {
            InitializeComponent();
        }

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private IntPtr _hWnd = IntPtr.Zero;
        private int _width = 1;
        private int _height = 1;
        private ScaleTransform? _scaleTransform = null;

        //表示・非表示とスケーリングの双方で使う
        private int _mouseStopTimeMillisec = 0;
        private int _prevMouseX = -1;
        private int _prevMouseY = -1;

        //表示・非表示の制御だけに使う
        private double _prevOpacity = 1.0;

        //スケーリングだけに使う。Sqrを使うと整数計算で閉じて都合がいいのでそうしている。
        private double _prevScale = 0.8;
        private int _mouseMoveDistanceSumSqr = 0;

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            _hWnd = new WindowInteropHelper(this).Handle;
            SetClickThrough(_hWnd);

            var rect = GetWindowRect(_hWnd);
            _width = rect.right - rect.left;
            _height = rect.bottom - rect.top;
            _scaleTransform = MainGrid.RenderTransform as ScaleTransform;

            Task.Run(async () => await LoopUpdateWindowPositionAsync(_cts.Token));
        }

        protected override void OnClosing(CancelEventArgs e) => _cts.Cancel();

        private void SetClickThrough(IntPtr hWnd)
        {
            uint exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
            SetWindowLong(hWnd, GWL_EXSTYLE, exStyle | WS_EX_TRANSPARENT);
        }

        private async Task LoopUpdateWindowPositionAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                Thread.Sleep(MouseTrackIntervalMillisec);
                //ここでマウス位置をとって移動
                await Dispatcher.BeginInvoke(
                    new Action(UpdatePointerPositionAndOpacity)
                    );
            }
        }

        private void UpdatePointerPositionAndOpacity()
        {
            bool isMouseMoved = CheckAndTrackMousePosition();

            //マウスが止まりっぱなしかどうかチェック
            if (isMouseMoved)
            {
                _mouseStopTimeMillisec = 0;
            }
            else if (_mouseStopTimeMillisec < MouseStopDisappearTimeMillisec)
            {
                _mouseStopTimeMillisec += MouseTrackIntervalMillisec;
            }

            //Opacityの更新
            {
                //ずっと止まってるならポインターを消す。そうじゃなければつける。
                //いずれもパッと変えると違和感あるのでLerpする。
                double goalOpacity =
                    (_mouseStopTimeMillisec < MouseStopDisappearTimeMillisec) ?
                    1.0 :
                    0.0;

                double nextOpacity = Lerp(_prevOpacity, goalOpacity, OpacityChangeLerpFactor);
                MainGrid.Opacity = nextOpacity;
                _prevOpacity = nextOpacity;
            }

            //Scaleの更新
            {
                //ユーザーがマウスをグリグリしなくなったと判別
                if (_mouseStopTimeMillisec > MouseStopScaleResetTimeMillisec)
                {
                    _mouseMoveDistanceSumSqr = 0;
                }

                double goalScale =
                    (_mouseMoveDistanceSumSqr > ScaleIncreaseMoveDistanceSqr) ?
                    ScaleWhenMoving :
                    ScaleWhenStop;
                double nextScale = Lerp(_prevScale, goalScale, ScaleChangeLerpFactor);
                if (_scaleTransform != null)
                {
                    _scaleTransform.ScaleX = nextScale;
                    _scaleTransform.ScaleY = nextScale;
                }
                _prevScale = nextScale;
            }
        }

        private bool CheckAndTrackMousePosition()
        {
            var cursorPos = GetWindowsMousePosition();
            if (cursorPos.X != _prevMouseX || cursorPos.Y != _prevMouseY)
            {
                SetWindowPosition(_hWnd, cursorPos.X - _width / 2, cursorPos.Y - _height / 2);

                if (_mouseMoveDistanceSumSqr < ScaleIncreaseMoveDistanceSqr)
                {
                    _mouseMoveDistanceSumSqr +=
                        (_prevMouseX - cursorPos.X) * (_prevMouseX - cursorPos.X) +
                        (_prevMouseY - cursorPos.Y) * (_prevMouseY - cursorPos.Y);
                }

                _prevMouseX = cursorPos.X;
                _prevMouseY = cursorPos.Y;

                return true;
            }
            else
            {
                return false;
            }
        }

        private static double Lerp(double a, double b, double rate)
            => a * (1.0 - rate) + b * rate;
    }
}
