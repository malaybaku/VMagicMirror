using System;
using System.Windows.Forms;

namespace Baku.VMagicMirror.GameInput
{
    [Serializable]
    public class KeyboardGameInputKeyAssign
    {
        public bool UseMouseLookAround = true;
        public GameInputButtonAction LeftClick;
        public GameInputButtonAction RightClick;
        public GameInputButtonAction MiddleClick;

        //よくあるやつなので + このキーアサインでは補助キーを無視したいのでShiftも特別扱い
        public bool UseWasdMove = true;
        public bool UseArrowKeyMove = true;
        public bool UseShiftRun = true;
        public bool UseSpaceJump = true;

        //NOTE: ShiftとSpaceは上記のフラグで設定される場合、下記のキーコードで指定しなくても適用されるのがto-be
        //これは後方互換性、およびShiftキーの取り回しがちょっと面倒=KeysではLShiftとRShiftが別扱いされてるのが理由
        public string JumpKeyCode = nameof(Keys.Space);
        public string RunKeyCode = "";
        public string CrouchKeyCode = "";

        public string TriggerKeyCode = "";
        public string PunchKeyCode = "";

        public static KeyboardGameInputKeyAssign LoadDefault() => new KeyboardGameInputKeyAssign();

        public void OverwriteKeyCodeIntToKeyName()
        {
            JumpKeyCode = ParseIntToKeyName(JumpKeyCode);
            RunKeyCode = ParseIntToKeyName(RunKeyCode);
            CrouchKeyCode = ParseIntToKeyName(CrouchKeyCode);
            TriggerKeyCode = ParseIntToKeyName(TriggerKeyCode);
            PunchKeyCode = ParseIntToKeyName(PunchKeyCode);
        }

        private static string ParseIntToKeyName(string key) =>
            string.IsNullOrEmpty(key) ? "" : 
            int.TryParse(key, out var value) ? ((Keys)value).ToString() : 
            "";
    }    
}

