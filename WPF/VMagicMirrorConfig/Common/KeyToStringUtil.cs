using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Input;

namespace Baku.VMagicMirrorConfig
{
    // ref: https://stackoverflow.com/questions/5825820/how-to-capture-the-character-on-different-locale-keyboards-in-wpf-c
    internal class KeyToStringUtil
    {
        private const uint MAPVK_VK_TO_VSC = 0x00;

        //VMagicMirrorでは`Shift + 5`のような表示がしたい (not `%` or `Shift + %`)ので、
        //UniCodeを拾うときはmodifier keyが入力されていないものとして扱う
        static readonly byte[] emptyKeyboardState = new byte[256];

        public static char GetCharFromKey(Key key)
        {
            var virtualKey = KeyInterop.VirtualKeyFromKey(key);
            var scanCode = MapVirtualKey((uint)virtualKey, MAPVK_VK_TO_VSC);
            var stringBuilder = new StringBuilder(2);

            var result = ToUnicode((uint)virtualKey, scanCode, emptyKeyboardState, stringBuilder, stringBuilder.Capacity, 0);
            switch (result)
            {
                case -1:
                    return ' ';
                case 0:
                    return ' ';
                case 1:
                    return stringBuilder[0];
                default:
                    return stringBuilder[0];
            }
        }

        [DllImport("user32.dll")]
        private static extern int ToUnicode(
            uint wVirtKey,
            uint wScanCode,
            byte[] lpKeyState,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)]
            StringBuilder pwszBuff,
            int cchBuff,
            uint wFlags);

        [DllImport("user32.dll")]
        private static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);
    }
}
