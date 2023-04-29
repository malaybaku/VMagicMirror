using System;
using System.Windows.Input;

namespace Baku.VMagicMirrorConfig
{
    public enum GameInputLocomotionStyle
    {
        //スティックの向きはアバターの移動方向そのものであり、かつアバターの体は同じ方向を向き続ける
        FirstPerson,
        //スティックの向きにアバターの体が向き直る。左右や奥にスティックを倒した場合、アバターの顔は見えない事もある。
        ThirdPerson,
        //横スクロール用の3人称挙動で、左か右のいずれかにしか向かない
        SideView2D,
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

        //NOTE: これらのxxxKeyCodeにはSystem.Windows.Forms.KeyをToStringしたものが入る。カラの場合、アサインが無いことを示す
        public string JumpKeyCode { get; set; } = "Space";
        public string RunKeyCode { get; set; } = "Shift";
        public string CrouchKeyCode { get; set; } = "C";

        public string TriggerKeyCode { get; set; } = "";
        public string PunchKeyCode { get; set; } = "";

        public static GameInputKeyboardKeyAssign LoadDefault() => new();

        //Unityに投げつける用に前処理したデータを生成する
        public GameInputKeyboardKeyAssign GetKeyCodeTranslatedData()
        {
            var result = new GameInputKeyboardKeyAssign()
            {
                UseMouseLookAround = UseMouseLookAround,
                LeftClick = LeftClick,
                RightClick = RightClick,
                MiddleClick = MiddleClick,
                UseWasdMove = UseWasdMove,
                UseArrowKeyMove = UseArrowKeyMove,
                UseShiftRun = UseShiftRun,
                UseSpaceJump = UseSpaceJump,
            };

            result.JumpKeyCode = TranslateKeyCode(JumpKeyCode);
            result.RunKeyCode = TranslateKeyCode(RunKeyCode);
            result.CrouchKeyCode = TranslateKeyCode(CrouchKeyCode);
            result.TriggerKeyCode = TranslateKeyCode(TriggerKeyCode);
            result.PunchKeyCode = TranslateKeyCode(PunchKeyCode);

            return result;
        }

        private static string TranslateKeyCode(string wpfKey)
        {
            if (string.IsNullOrEmpty(wpfKey))
            {
                return "";
            }

            if (!Enum.TryParse<Key>(wpfKey, out var key))
            {
                return "";
            }

            //渋い気もするが、このkの整数値をテキストで書き込む
            // -> Unity側はこの整数値をWindows.Forms.Keyとして解釈する
            var k = KeyInterop.VirtualKeyFromKey(key);
            return k.ToString();
        }
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
        public int LocomotionStyleValue { get; set; } = 0;

        public GameInputKeyboardKeyAssign KeyboardKeyAssign { get; set; } = new();
        public GameInputGamepadKeyAssign GamepadKeyAssign { get; set; } = new();
    }
}
