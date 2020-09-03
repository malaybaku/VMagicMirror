using System.Runtime.InteropServices;
using XinputGamePad;
    
namespace Baku.VMagicMirror
{
    /// <summary> XInputの入力を必要最小限引っ張ってくるクラス。 </summary>
    /// <remarks>
    /// APIが奇妙なのはXInputGamepad(OSS)を置き換える目的で書いてるからです。
    /// </remarks>
    public class LegacyXInputCapture
    {
        public void Update()
        {
            DllConst.Capture();
        }

        public int GetButtonStates() => DllConst.GetButtons(0);
        public int GetLeftTrigger() => DllConst.GetLeftTrigger(0);
        public int GetRightTrigger() => DllConst.GetRightTrigger(0);
        public int GetThumbLX() => DllConst.GetThumbLX(0);
        public int GetThumbLY() => DllConst.GetThumbLY(0);
        public int GetThumbRX() => DllConst.GetThumbRX(0);
        public int GetThumbRY() => DllConst.GetThumbRY(0);

    }
}