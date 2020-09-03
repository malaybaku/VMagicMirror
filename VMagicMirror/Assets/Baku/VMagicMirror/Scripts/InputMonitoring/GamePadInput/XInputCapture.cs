using System.Runtime.InteropServices;

namespace Baku.VMagicMirror
{
    /// <summary> XInputの入力を必要最小限引っ張ってくるクラス。 </summary>
    /// <remarks>
    /// APIがちょっとヘンテコですが、これはXInputGamepad(OSS)を置き換える目的で書いたためです。
    /// </remarks>
    public class XInputCapture
    {
        private XINPUT_STATE _state;

        public void Update() => XInputGetState(0, ref _state);

        public int GetButtonStates() => _state.GamePad.wButtons;
        public int GetLeftTrigger() => _state.GamePad.bLeftTrigger;
        public int GetRightTrigger() => _state.GamePad.bRightTrigger;
        public int GetThumbLX() => _state.GamePad.sThumbLX;
        public int GetThumbLY() => _state.GamePad.sThumbLY;
        public int GetThumbRX() => _state.GamePad.sThumbRX;
        public int GetThumbRY() => _state.GamePad.sThumbRY;

        [StructLayout(LayoutKind.Sequential)]
        struct XINPUT_STATE
        {
            public uint dwPacketNumber;
            public readonly XINPUT_GAMEPAD GamePad;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct XINPUT_GAMEPAD
        {
            public ushort wButtons;
            public byte bLeftTrigger;
            public byte bRightTrigger;
            public short sThumbLX;
            public short sThumbLY;
            public short sThumbRX;
            public short sThumbRY;
        }

        [DllImport("Xinput1_4.dll")]
        private static extern uint XInputGetState(uint index, ref XINPUT_STATE result);
        
        public static class Buttons
        {
            public const int DPAD_UP          = 0x0001;
            public const int DPAD_DOWN        = 0x0002;
            public const int DPAD_LEFT        = 0x0004;
            public const int DPAD_RIGHT       = 0x0008;
            public const int START            = 0x0010;
            public const int BACK             = 0x0020;
            public const int LEFT_THUMB       = 0x0040;
            public const int RIGHT_THUMB      = 0x0080;
            public const int LEFT_SHOULDER    = 0x0100;
            public const int RIGHT_SHOULDER   = 0x0200;
            public const int A                = 0x1000;
            public const int B                = 0x2000;
            public const int X                = 0x4000;
            public const int Y                = 0x8000;
        }
    }
}
