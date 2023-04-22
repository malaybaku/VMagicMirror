namespace Baku.VMagicMirror.GameInput
{
    //NOTE: WPF側の定義と揃えてる + 設定ファイルにも載る名前であることに注意

    public enum GameInputLocomotionStyle
    {
        FirstPerson = 0,
        ThirdPerson = 1,
        SideView2D = 2,
    }

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
}
