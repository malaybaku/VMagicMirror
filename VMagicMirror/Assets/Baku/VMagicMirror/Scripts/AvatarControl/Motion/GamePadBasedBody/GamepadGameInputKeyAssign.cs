using System;
using UnityEngine;

namespace Baku.VMagicMirror.GameInput
{
    //WPF側の定義と揃えてることに注意(デフォルト値も含めて)

    [Serializable]
    public class GameInputCustomAction
    {
        public string CustomKey;

        public static GameInputCustomAction Empty() => new() { CustomKey = "" };
    }
    
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

        [SerializeField] private GameInputCustomAction CustomButtonA;
        [SerializeField] private GameInputCustomAction CustomButtonB;
        [SerializeField] private GameInputCustomAction CustomButtonX;
        [SerializeField] private GameInputCustomAction CustomButtonY;

        [SerializeField] private GameInputCustomAction CustomButtonLButton;
        [SerializeField] private GameInputCustomAction CustomButtonLTrigger;
        [SerializeField] private GameInputCustomAction CustomButtonRButton;
        [SerializeField] private GameInputCustomAction CustomButtonRTrigger;

        [SerializeField] private GameInputCustomAction CustomButtonView;
        [SerializeField] private GameInputCustomAction CustomButtonMenu;

        public string CustomButtonAKey => CustomButtonA?.CustomKey ?? "";
        public string CustomButtonBKey => CustomButtonB?.CustomKey ?? "";
        public string CustomButtonXKey => CustomButtonX?.CustomKey ?? "";
        public string CustomButtonYKey => CustomButtonY?.CustomKey ?? "";

        public string CustomButtonLButtonKey => CustomButtonLButton?.CustomKey ?? "";
        public string CustomButtonLTriggerKey => CustomButtonLTrigger?.CustomKey ?? "";
        public string CustomButtonRButtonKey => CustomButtonRButton?.CustomKey ?? "";
        public string CustomButtonRTriggerKey => CustomButtonRTrigger?.CustomKey ?? "";

        public string CustomButtonViewKey => CustomButtonView?.CustomKey ?? "";
        public string CustomButtonMenuKey => CustomButtonMenu?.CustomKey ?? "";

        public static GamepadGameInputKeyAssign LoadDefault() => new();
    }
}
