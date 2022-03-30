namespace Baku.VMagicMirrorConfig
{
    class GamepadSettingModel : SettingModelBase<GamepadSetting>
    {
        static class LeanModeNames
        {
            public const string GamepadLeanNone = nameof(GamepadLeanNone);
            public const string GamepadLeanLeftStick = nameof(GamepadLeanLeftStick);
            public const string GamepadLeanRightStick = nameof(GamepadLeanRightStick);
            public const string GamepadLeanLeftButtons = nameof(GamepadLeanLeftButtons);
        }

        public GamepadSettingModel() : this(ModelResolver.Instance.Resolve<IMessageSender>())
        {
        }

        public GamepadSettingModel(IMessageSender sender) : base(sender)
        {
            var s = GamepadSetting.Default;
            var factory = MessageFactory.Instance;


            GamepadEnabled = new RProperty<bool>(s.GamepadEnabled, b =>
            {
                SendMessage(factory.EnableGamepad(b));
                if (!b && GamepadVisibility != null)
                {
                    //読み込み無効なら表示する価値は無いであろう、と判断
                    GamepadVisibility.Value = false;
                }
            });

            PreferDirectInputGamepad = new RProperty<bool>(s.PreferDirectInputGamepad, b => SendMessage(factory.PreferDirectInputGamepad(b)));
            GamepadVisibility = new RProperty<bool>(s.GamepadVisibility, b => SendMessage(factory.GamepadVisibility(b)));

            //排他になるように制御
            //TODO: RadioButtonの要請により、「一瞬たりとてフラグが2つ同時に立つのは許さん」みたいな要件もありうるので試しておくこと。
            //NOTE: nullableっぽい書き方を一部してるが実際はnullableではない
            GamepadLeanNone = new RProperty<bool>(s.GamepadLeanNone, b =>
            {
                if (b)
                {
                    SendMessage(factory.GamepadLeanMode(LeanModeNames.GamepadLeanNone));
                    GamepadLeanLeftStick?.Set(false);
                    GamepadLeanRightStick?.Set(false);
                    GamepadLeanLeftButtons?.Set(false);
                }
            });
            GamepadLeanLeftStick = new RProperty<bool>(s.GamepadLeanLeftStick, b =>
            {
                if (b)
                {
                    SendMessage(factory.GamepadLeanMode(LeanModeNames.GamepadLeanLeftStick));
                    GamepadLeanNone.Value = false;
                    GamepadLeanRightStick?.Set(false);
                    GamepadLeanLeftButtons?.Set(false);
                }
            });
            GamepadLeanRightStick = new RProperty<bool>(s.GamepadLeanRightStick, b =>
            {
                if (b)
                {
                    SendMessage(factory.GamepadLeanMode(LeanModeNames.GamepadLeanRightStick));
                    GamepadLeanNone.Value = false;
                    GamepadLeanLeftStick.Value = false;
                    GamepadLeanLeftButtons?.Set(false);
                }
            });
            GamepadLeanLeftButtons = new RProperty<bool>(s.GamepadLeanLeftButtons, b =>
            {
                if (b)
                {
                    SendMessage(factory.GamepadLeanMode(LeanModeNames.GamepadLeanLeftButtons));
                    GamepadLeanNone.Value = false;
                    GamepadLeanLeftStick.Value = false;
                    GamepadLeanRightStick.Value = false;
                }
            });

            GamepadLeanReverseHorizontal = new RProperty<bool>(
                s.GamepadLeanReverseHorizontal, b => SendMessage(factory.GamepadLeanReverseHorizontal(b))
                );
            GamepadLeanReverseVertical = new RProperty<bool>(s.GamepadLeanReverseVertical, b => SendMessage(factory.GamepadLeanReverseVertical(b)));
        }

        public RProperty<bool> GamepadEnabled { get; }
        public RProperty<bool> PreferDirectInputGamepad { get; }
        public RProperty<bool> GamepadVisibility { get; }

        //NOTE: 本来ならEnum値1つで管理する方がよいが、TwoWayバインディングが簡便になるのでbool4つで代用していた経緯があり、こういう持ち方。

        //モデル層では「1つの値がtrueになるとき他を全部falseにする」という措置を行わないといけないため、RPropertyを使わずに捌く
        public RProperty<bool> GamepadLeanNone { get; }
        public RProperty<bool> GamepadLeanLeftButtons { get; }
        public RProperty<bool> GamepadLeanLeftStick { get; }
        public RProperty<bool> GamepadLeanRightStick { get; }

        public RProperty<bool> GamepadLeanReverseHorizontal { get; }
        public RProperty<bool> GamepadLeanReverseVertical { get; }

        public override void ResetToDefault() => Load(GamepadSetting.Default);

        public void ResetVisibility()
        {
            GamepadVisibility.Value = false;
        }
    }
}
