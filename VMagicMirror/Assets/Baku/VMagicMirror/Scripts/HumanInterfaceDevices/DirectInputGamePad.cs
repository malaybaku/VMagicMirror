using System;
using System.Linq;
using SharpDX.DirectInput;
using DeviceType = SharpDX.DirectInput.DeviceType;

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
            LogOutput.Instance.Write("Connect to Gamepad..");
            Stop();
            _directInput = new DirectInput();

            //使えそうなゲームパッド or ジョイスティックを探す
            //Gamepad, Joystick, どっちも無ければGameControlクラスを順に探します
            var devices = _directInput
                .GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices)
                .Concat(_directInput.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AllDevices))
                .Concat(_directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AllDevices))
                .ToList();

            if (devices.Count == 0)
            {
                LogOutput.Instance.Write("No Gamepad Found");
                _directInput?.Dispose();
                _directInput = null;
                return;
            }

            var joystickGuid = devices[0].InstanceGuid;
            _joystick = new Joystick(_directInput, joystickGuid);
            LogOutput.Instance.Write("Gamepad Found, name = " + _joystick.Properties.ProductName);
                
            try
            {
                //初期設定: バックグラウンドで非占有にすることで常時読み取れるようにする
                //CAUTION: コントローラによってはこの設定を無視する。例えばXBox OneコントローラはForegroundじゃないとダメ。
                _joystick.SetCooperativeLevel(mainWindowHandle,
                    CooperativeLevel.Background | CooperativeLevel.NonExclusive);
                _joystick.Acquire();
                _joystickReady = true;
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }
        
        /// <summary> データを読み取っている場合、それを停止する </summary>
        public void Stop()
        {
            LogOutput.Instance.Write("Stop Reading Gamepad");
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

            //NOTE: ボタンの対応付けはDUAL SHOCK 4を手元環境でさした時のレイアウトに基づきます。
            CurrentState.X = state.Buttons[0];
            CurrentState.A = state.Buttons[1];
            CurrentState.B = state.Buttons[2];
            CurrentState.Y = state.Buttons[3];
            
            CurrentState.L1 = state.Buttons[4];
            CurrentState.R1 = state.Buttons[5];

            CurrentState.L2 = state.Buttons[6];
            CurrentState.R2 = state.Buttons[7];
            
            //NOTE: 物理的にはShare[8]とOption[9]ボタンがSelect, Startに近い位置にあるが、
            //かなり押しにくいので代わりにタッチ部分[13]もStartボタン扱いするよ、という意図です
            //※PSボタン[12]は押すとSteamが起動したりするので割り当てナシです。
            CurrentState.Select = state.Buttons[8];
            CurrentState.Start = state.Buttons[9] || state.Buttons[13];
            
            CurrentState.L3 = state.Buttons[10];
            CurrentState.R3 = state.Buttons[11];

            CurrentState.LeftX = state.X - 32767;
            CurrentState.LeftY = 32767 - state.Y;
            CurrentState.RightX = state.Z - 32767;
            CurrentState.RightY = 32767 - state.RotationZ;

            //NOTE: 通常ないが、十字キーのないコントローラあると困るな～というガードつき
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

    