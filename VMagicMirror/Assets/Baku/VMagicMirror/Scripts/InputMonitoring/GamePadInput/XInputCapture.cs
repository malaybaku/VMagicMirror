using System.Runtime.InteropServices;

namespace Baku.VMagicMirror
{
    /// <summary> XInputの入力を必要最小限引っ張ってくるクラス。 </summary>
    /// <remarks>
    /// APIが奇妙なのはXInputGamepad(OSS)を置き換える目的で書いてるからです。
    /// </remarks>
    public class XInputCapture
    {
        public void Update()
        {
            XInputDll.XInputGetState(0, ref _state);
        }
        private XINPUT_STATE _state;

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

        static class XInputDll
        {
            [DllImport("Xinput1_4.dll")]
            public static extern uint XInputGetState(uint index, ref XINPUT_STATE result);
        }
    }
}
