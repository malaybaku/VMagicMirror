using System;

namespace Baku.VMagicMirror.GameInput
{
    //WPF側の定義と揃えてることに注意(デフォルト値も含めて)

    [Serializable]
    public class GamepadGameInputKeyAssign
    {
        public GameInputButtonAction ButtonA = GameInputButtonAction.Jump;
        public GameInputButtonAction ButtonB;
        public GameInputButtonAction ButtonX;
        public GameInputButtonAction ButtonY;

        //NOTE: LTriggerはボタンと連続値どっちがいいの、みたいな話もある
        public GameInputButtonAction ButtonLButton;
        public GameInputButtonAction ButtonLTrigger;
        public GameInputButtonAction ButtonRButton;
        public GameInputButtonAction ButtonRTrigger = GameInputButtonAction.Trigger;

        public GameInputButtonAction ButtonView;
        public GameInputButtonAction ButtonMenu;

        public GameInputStickAction DPadLeft;
        public GameInputStickAction StickLeft;
        public GameInputStickAction StickRight;

        public static GamepadGameInputKeyAssign LoadDefault() => new GamepadGameInputKeyAssign();
    }
}
