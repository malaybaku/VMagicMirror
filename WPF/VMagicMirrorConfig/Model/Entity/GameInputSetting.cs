namespace Baku.VMagicMirrorConfig
{

    public enum GameInputStickAction
    {
        None,
        Move,
        LookAround,
    }

    public enum GameInputButtonAction
    {
        None,
        Jump,
        Crouch,
        Run,
        Trigger,
        Punch,
    }

    public enum GameInputGamepadButton
    {
        None,
        A,
        B,
        X,
        Y,
        LB,
        RB,
        LTrigger,
        RTrigger,
        //Left
        View,
        //Right
        Menu,
        //NOTE: Stick Pressを含めてもいいが、一旦無しにしておく
    }

    public enum GameInputGamepadStick
    {
        None,
        Left,
        Right,
        DPadLeft,
    }


    public enum GameInputMouseButton
    {
        None,
        Left,
        Right,
        Middle,
    }


    //Entityに移動していいんでは
    public class GameInputGamepadKeyAssign
    {
        public GameInputButtonAction ButtonA { get; set; } = GameInputButtonAction.Jump;
        public GameInputButtonAction ButtonB { get; set; }
        public GameInputButtonAction ButtonX { get; set; }
        public GameInputButtonAction ButtonY { get; set; }

        //NOTE: LTriggerはボタンと連続値どっちがいいの、みたいな話もある
        public GameInputButtonAction ButtonLButton { get; set; }
        public GameInputButtonAction ButtonLTrigger { get; set; }
        public GameInputButtonAction ButtonRButton { get; set; }
        public GameInputButtonAction ButtonRTrigger { get; set; } = GameInputButtonAction.Trigger;

        public GameInputButtonAction ButtonView { get; set; }
        public GameInputButtonAction ButtonMenu { get; set; }

        public GameInputStickAction DPadLeft { get; set; }
        public GameInputStickAction StickLeft { get; set; } = GameInputStickAction.Move;
        public GameInputStickAction StickRight { get; set; } = GameInputStickAction.LookAround;

        //NOTE: 「戻り値は書き換えないでね」系のやつ
        public static GameInputGamepadKeyAssign Default { get; } = new();
    }

    public class GameInputKeyboardKeyAssign
    {
        public bool UseMouseLookAround { get; set; } = true;
        public GameInputButtonAction LeftClick { get; set; }
        public GameInputButtonAction RightClick { get; set; }
        public GameInputButtonAction MiddleClick { get; set; }

        //よくあるやつなので + このキーアサインでは補助キーを無視したいのでShiftも特別扱い
        public bool UseWasdMove { get; set; } = true;
        public bool UseArrowKeyMove { get; set; } = true;
        public bool UseShiftRun { get; set; } = true;
        public bool UseSpaceJump { get; set; } = true;

        public string JumpKeyCode { get; set; } = "Space";
        public string RunKeyCode { get; set; } = "Shift";
        public string CrouchKeyCode { get; set; } = "C";

        public string TriggerKeyCode { get; set; } = "";
        public string PunchKeyCode { get; set; } = "";

        public static GameInputKeyboardKeyAssign LoadDefault() => new();
    }


    /// <summary>
    /// JSONシリアライズを想定した、ゲーム入力の設定内容。
    /// 他の設定群とは独立に管理される想定 (アバターよりもプレイするゲームとかに依存するはずなため)
    /// </summary>
    public class GameInputSetting
    {
        /// <summary>
        /// NOTE: 規約としてこの値は書き換えません。
        /// デフォルト値を参照したい人が、プロパティ読み込みのみの為だけに使います。
        /// </summary>
        public static GameInputSetting Default { get; } = new();

        public bool GamepadEnabled { get; set; } = true;
        public bool KeyboardEnabled { get; set; } = true;
        public bool AlwaysRun { get; set; } = true;

        public GameInputKeyboardKeyAssign KeyboardKeyAssign { get; } = new();
        public GameInputGamepadKeyAssign GamepadKeyAssign { get; } = new();
    }
}
