using R3;
using Zenject;

namespace Baku.VMagicMirror.GameInput
{
    public class GameInputSourceSet : PresenterBase
    {
        private readonly ReactiveProperty<bool> _useGamePad = new ReactiveProperty<bool>(true);
        private readonly ReactiveProperty<bool> _useKeyboard = new ReactiveProperty<bool>(true);

        public IGameInputSource[] Sources { get; }

        private readonly IMessageReceiver _receiver;
        private readonly BodyMotionModeController _bodyMotionModeController;
        private readonly GamepadGameInputSource _gamePadInput;
        private readonly KeyboardGameInputSource _keyboardInput;
        private readonly CompositeDisposable _disposable = new CompositeDisposable();

        [Inject]
        public GameInputSourceSet(
            IMessageReceiver receiver,
            BodyMotionModeController bodyMotionModeController,
            GamepadGameInputSource gamePadInput, 
            KeyboardGameInputSource keyboardInput)
        {
            _receiver = receiver;
            _bodyMotionModeController = bodyMotionModeController;
            _gamePadInput = gamePadInput;
            _keyboardInput = keyboardInput;
            Sources = new IGameInputSource[] { _gamePadInput, _keyboardInput };
        }

        public override void Initialize()
        {
            _receiver.AssignCommandHandler(
                VmmCommands.UseGamepadForGameInput,
                command => _useGamePad.Value = command.ToBoolean()
            );

            _receiver.AssignCommandHandler(
                VmmCommands.UseKeyboardForGameInput,
                command => _useKeyboard.Value = command.ToBoolean()
            );

            _bodyMotionModeController.MotionMode
                .CombineLatest(
                    _useGamePad, 
                    (mode, useGamePad) => mode == BodyMotionMode.GameInputLocomotion && useGamePad)
                .Subscribe(v => _gamePadInput.SetActive(v))
                .AddTo(_disposable);

            _bodyMotionModeController.MotionMode
                .CombineLatest(
                    _useKeyboard, 
                    (mode, useKeyboard) => mode == BodyMotionMode.GameInputLocomotion && useKeyboard)
                .Subscribe(v => _keyboardInput.SetActive(v))
                .AddTo(_disposable);
        }
    }
}
