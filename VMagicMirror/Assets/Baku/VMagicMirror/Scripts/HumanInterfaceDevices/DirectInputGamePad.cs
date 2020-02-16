using System;
using SharpDX.DirectInput;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 本来はXInputで取得するはずのゲームパッド状態を代わりにDirectInputで取得するやつだよ
    /// </summary>
    public class DirectInputGamePad
    {
        public GamepadState CurrentState { get; } = new GamepadState();

        private DirectInput _directInput = null;
        private Joystick _joystick = null;
        private bool _joystickReady = false;
        
        /// <summary>
        /// デバイスに接続して読み取れる状態にする。
        /// </summary>
        /// <param name="mainWindowHandle"></param>
        public void ConnectToDevice(IntPtr mainWindowHandle)
        {
            Stop();
            _directInput = new DirectInput();

            //使えそうなゲームパッド or ジョイスティックを探す
            var joystickGuid = Guid.Empty;
            foreach (var deviceInstance in _directInput.GetDevices(SharpDX.DirectInput.DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices))
            {
                joystickGuid = deviceInstance.InstanceGuid;
            }
            if (joystickGuid == Guid.Empty)
            {
                foreach (var deviceInstance in _directInput.GetDevices(SharpDX.DirectInput.DeviceType.Joystick, DeviceEnumerationFlags.AllDevices))
                {
                    joystickGuid = deviceInstance.InstanceGuid;
                }
            }

            //ないので諦める
            if (joystickGuid == Guid.Empty)
            {
                _directInput?.Dispose();
                _directInput = null;
            }

            _joystick = new Joystick(_directInput, joystickGuid);

            //初期設定: バックグラウンドで非占有にすることで常時読み取れるようにする
            _joystick.SetCooperativeLevel(mainWindowHandle, CooperativeLevel.Background | CooperativeLevel.NonExclusive);
            _joystick.Acquire();

            _joystickReady = true;
        }
        
        /// <summary> データを読み取っている場合、それを停止する </summary>
        public void Stop()
        {
            _joystick?.Dispose();
            _joystick = null;
            
            _directInput?.Dispose();
            _directInput = null;

            _joystickReady = false;
        }
        
        /// <summary> コントローラに接続済みの場合、そのコントローラの状態を読み取る。 </summary>
        public void Update()
        {
            if (!_joystickReady)
            {
                CurrentState.Reset();
                return;
            }

            _joystick.Poll();

            var state = _joystick.GetCurrentState();
            CurrentState.IsValid = true;
            
            CurrentState.A = state.Buttons[0];
            CurrentState.B = state.Buttons[1];
            CurrentState.X = state.Buttons[2];
            CurrentState.Y = state.Buttons[3];
            
            CurrentState.L1 = state.Buttons[4];
            CurrentState.R1 = state.Buttons[5];
            
            CurrentState.Select = state.Buttons[6];
            CurrentState.Start = state.Buttons[7];
            
            CurrentState.L3 = state.Buttons[8];
            CurrentState.R3 = state.Buttons[9];

            CurrentState.LeftX = state.X - 32767;
            CurrentState.LeftY = 32767 - state.Y;
            CurrentState.RightX = state.RotationX - 32767;
            CurrentState.RightY = 32767 - state.RotationY;

            //TODO: L2とR2は本物のDirectInputなパッドだとボタン[10],[11]に割り当たってそうな気がするので保留。
            //いったん「反応しませ～ん！」でリリースするのもアリだと思いますよ

            //NOTE: 通常ないが、十字キーのないコントローラあると困るな～というガードです
            if (state.PointOfViewControllers.Length > 0)
            {
                SetPov(state.PointOfViewControllers[0]);
            }
        }

        private void SetPov(int povValue)
        {
            if (povValue < 0)
            {
                CurrentState.Up = false;
                CurrentState.Right = false;
                CurrentState.Down = false;
                CurrentState.Left = false;
                return;
            }

            //NOTE: povValue == -1は入力なしで、それ以外は0.01deg単位の値が降ってくる。
            //ただし、実際は方向キーなので以下8つの値のどれかが返ってくる。はず。
            //0, 4500, 9000, 13500, 18000, 21500, 27000, 31500
            CurrentState.Up = (povValue < 9000 || povValue > 27000);
            CurrentState.Right = (povValue > 0 && povValue < 18000);
            CurrentState.Down = (povValue > 9000 && povValue < 27000);
            CurrentState.Left = povValue > 18000;
        }
    }
}
