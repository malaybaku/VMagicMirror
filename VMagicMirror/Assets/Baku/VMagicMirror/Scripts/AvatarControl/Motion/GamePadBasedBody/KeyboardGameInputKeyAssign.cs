using System;
using UnityEngine;

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

        //NOTE: 初期リリース時点では以下の値は使用せず、

        public string JumpKeyCode = nameof(KeyCode.Space);
        //雑記: しれっと書いてるがShiftはLeftShift / RightShiftを区別しない必要があり、意外と面倒
        public string RunKeyCode = "Shift";
        public string CrouchKeyCode = "";

        public string TriggerKeyCode = "";
        public string PunchKeyCode = "";

        public static KeyboardGameInputKeyAssign LoadDefault() => new KeyboardGameInputKeyAssign();
    }    
}

