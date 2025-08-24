using System;
using Baku.VMagicMirror.GameInput;
using R3;
using Zenject;

namespace Baku.VMagicMirror
{
    public enum BodyMotionMode
    {
        /// <summary> 触れたデバイスを操作するモード </summary>
        Default,    
        /// <summary> ほぼ棒立ちをキープするモード </summary>
        StandingOnly,
        /// <summary> 入力を使ってゲームキャラっぽい動きをするモード </summary>
        GameInputLocomotion,
    }

    public class BodyMotionModeController : IInitializable, IDisposable
    {
        [Inject]
        public BodyMotionModeController(IMessageReceiver receiver)
        {
            _receiver = receiver;
        }

        private readonly IMessageReceiver _receiver;
        private readonly ReactiveProperty<bool> _enableNoHandTrackMode = new(false);
        private readonly ReactiveProperty<bool> _enableGameInputLocomotionMode = new(false);

        private readonly ReactiveProperty<BodyMotionMode> _motionMode = new(BodyMotionMode.Default);
        public ReadOnlyReactiveProperty<BodyMotionMode> MotionMode => _motionMode;

        private readonly ReactiveProperty<GameInputLocomotionStyle> _gameInputLocomotionStyle =
            new(GameInputLocomotionStyle.FirstPerson);
        public ReadOnlyReactiveProperty<GameInputLocomotionStyle> CurrentGameInputLocomotionStyle =>
            _gameInputLocomotionStyle;

        private readonly ReactiveProperty<GamepadMotionModes> _gamepadMotionMode = new(GamepadMotionModes.Gamepad);
        //NOTE: 名前がちょっとややこしいが、GameInputModeじゃないほうの「ゲームパッドを何かしら掴んでるモーションの種類」のほう
        public ReadOnlyReactiveProperty<GamepadMotionModes> GamepadMotionMode => _gamepadMotionMode;

        private readonly ReactiveProperty<KeyboardAndMouseMotionModes> _keyboardAndMouseMotionMode
            = new(KeyboardAndMouseMotionModes.KeyboardAndTouchPad);
        //NOTE: Noneという値も入る(Noneになると「キー入力は監視はしてるけどモーション的には無いのと同様に扱う」という状態でNoneになる)ことに注意
        public ReadOnlyReactiveProperty<KeyboardAndMouseMotionModes> KeyboardAndMouseMotionMode
            => _keyboardAndMouseMotionMode;
        
        private IDisposable _disposable;

        public void Initialize()
        {
            _receiver.AssignCommandHandler(
                VmmCommands.SetGamepadMotionMode,
                command =>
                {
                    var rawValue = command.ToInt();
                    _gamepadMotionMode.Value = (rawValue >= 0 && rawValue < (int)GamepadMotionModes.Unknown)
                        ? (GamepadMotionModes)rawValue
                        : GamepadMotionModes.Unknown;
                });

            _receiver.AssignCommandHandler(
                VmmCommands.EnableNoHandTrackMode,
                command => _enableNoHandTrackMode.Value = command.ToBoolean()
            );

            _receiver.AssignCommandHandler(
                VmmCommands.EnableGameInputLocomotionMode,
                command => _enableGameInputLocomotionMode.Value = command.ToBoolean()
            );
         
            _receiver.AssignCommandHandler(
                VmmCommands.SetKeyboardAndMouseMotionMode,
                message => SetKeyboardAndMouseMotionMode(message.ToInt())
            );
            
            _disposable = _enableNoHandTrackMode
                .CombineLatest(
                    _enableGameInputLocomotionMode,
                    (noHandTrack, gameInputLocomotion) => (noHandTrack, gameInputLocomotion)
                )
                .Subscribe(modes =>
                {
                    _motionMode.Value =
                        modes.gameInputLocomotion ? BodyMotionMode.GameInputLocomotion :
                        modes.noHandTrack ? BodyMotionMode.StandingOnly :
                        BodyMotionMode.Default;
                });
            
            _receiver.AssignCommandHandler(
                VmmCommands.SetGameInputLocomotionStyle,
                command => SetGameInputLocomotionStyle(command.ToInt()));
        }

        private void SetKeyboardAndMouseMotionMode(int modeIndex)
        {
            if (modeIndex is >= (int) KeyboardAndMouseMotionModes.None and < (int) KeyboardAndMouseMotionModes.Unknown)
            {
                _keyboardAndMouseMotionMode.Value = (KeyboardAndMouseMotionModes) modeIndex;
            }
        }

        private void SetGameInputLocomotionStyle(int value)
        {
            _gameInputLocomotionStyle.Value = value switch
            {
                0 => GameInputLocomotionStyle.FirstPerson,
                1 => GameInputLocomotionStyle.ThirdPerson,
                2 => GameInputLocomotionStyle.SideView2D,
                //NOTE: 破綻防止ということでデフォルト値にフォールバック
                _ => GameInputLocomotionStyle.FirstPerson,
            };
        }

        public void Dispose()
        {
            _disposable?.Dispose();
            _disposable = null;
        }
    }
    
    /// <summary>
    /// ゲームパッド由来のモーションをどういう見た目で反映するか、というオプション。
    /// </summary>
    /// <remarks>
    /// どれを選んでいるにせよ、Word to Motionをゲームパッドでやっている間は処理が止まるなどの基本的な特徴は共通
    /// </remarks>
    public enum GamepadMotionModes
    {
        /// <summary> 普通のゲームパッド </summary>
        Gamepad = 0,
        /// <summary> アケコン </summary>
        ArcadeStick = 1,
        /// <summary> 車のハンドルっぽいやつ </summary>
        CarController = 2,
        /// <summary> 不明なため未サポート </summary>
        Unknown = 3,
        // /// <summary> ガンコン </summary>
        // GunController = 2,
    }
}
