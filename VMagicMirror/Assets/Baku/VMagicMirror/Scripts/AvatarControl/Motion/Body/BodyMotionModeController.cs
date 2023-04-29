using System;
using Baku.VMagicMirror.GameInput;
using UniRx;
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
        public BodyMotionModeController(IMessageReceiver receiver)
        {
            _receiver = receiver;
        }

        private readonly IMessageReceiver _receiver;
        private readonly ReactiveProperty<bool> _enableNoHandTrackMode = 
            new ReactiveProperty<bool>(false);
        private readonly ReactiveProperty<bool> _enableGameInputLocomotionMode = 
            new ReactiveProperty<bool>(false);

        private readonly ReactiveProperty<BodyMotionMode> _motionMode =
            new ReactiveProperty<BodyMotionMode>(BodyMotionMode.Default);
        public IReadOnlyReactiveProperty<BodyMotionMode> MotionMode => _motionMode;

        private readonly ReactiveProperty<GameInputLocomotionStyle> _gameInputLocomotionStyle =
            new ReactiveProperty<GameInputLocomotionStyle>(GameInputLocomotionStyle.FirstPerson);
        public IReadOnlyReactiveProperty<GameInputLocomotionStyle> CurrentGameInputLocomotionStyle =>
            _gameInputLocomotionStyle;

        private IDisposable _disposable;

        public void Initialize()
        {
            _receiver.AssignCommandHandler(
                VmmCommands.EnableNoHandTrackMode,
                command => _enableNoHandTrackMode.Value = command.ToBoolean()
            );

            _receiver.AssignCommandHandler(
                VmmCommands.EnableGameInputLocomotionMode,
                command => _enableGameInputLocomotionMode.Value = command.ToBoolean()
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
}
