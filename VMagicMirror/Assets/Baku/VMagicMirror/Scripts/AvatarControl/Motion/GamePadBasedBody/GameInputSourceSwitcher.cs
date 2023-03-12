using UniRx;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.GameInput
{
    //NOTE: MonoBehaviourじゃなくていいんだけど[serializeField]からいじりたいので一時的に…
    public class GameInputSourceSwitcher : MonoBehaviour, IGameInputSourceSwitcher
    {
        public enum GameInputSourceType
        {
            None,
            GamePad,
            Keyboard,
        }

        [SerializeField] private GameInputSourceType sourceType = GameInputSourceType.None;

        private readonly ReactiveProperty<bool> _isActive = new ReactiveProperty<bool>(false);
        public IReadOnlyReactiveProperty<bool> IsActive => _isActive;

        private readonly ReactiveProperty<bool> _useGamePad = new ReactiveProperty<bool>(true);
        private readonly ReactiveProperty<bool> _useKeyboard = new ReactiveProperty<bool>(true);

        private readonly ReactiveProperty<IGameInputSource> _source
            = new ReactiveProperty<IGameInputSource>(EmptyGameInputSource.Instance);
        //TODO: ここで返す内容はGamePadとKeyboardが混ざっててほしい
        public IReadOnlyReactiveProperty<IGameInputSource> Source => _source;

        private IMessageReceiver _receiver;
        private BodyMotionModeController _bodyMotionModeController;
        private GamepadGameInputSource _gamePadInput;
        private KeyboardGameInputSource _keyboardInput;

        [Inject]
        public void Construct(
            IMessageReceiver receiver,
            BodyMotionModeController bodyMotionModeController,
            GamepadGameInputSource gamePadInput, 
            KeyboardGameInputSource keyboardInput)
        {
            _receiver = receiver;
            _bodyMotionModeController = bodyMotionModeController;
            _gamePadInput = gamePadInput;
            _keyboardInput = keyboardInput;
            //TODO: receiverの指示ベースでどれを使うか切り替える
        }

        private void Start()
        {
            _bodyMotionModeController.MotionMode
                .Subscribe(mode => _isActive.Value = mode == BodyMotionMode.GameInputLocomotion)
                .AddTo(this);
            
            _receiver.AssignCommandHandler(
                VmmCommands.UseGamePadForGameInput,
                command => _useGamePad.Value = command.ToBoolean()
            );

            _receiver.AssignCommandHandler(
                VmmCommands.UseKeyboardForGameInput,
                command => _useKeyboard.Value = command.ToBoolean()
            );

            _isActive
                .CombineLatest(_useGamePad, (x, y) => x && y)
                .Subscribe(v => _gamePadInput.SetActive(v))
                .AddTo(this);

            _isActive
                .CombineLatest(_useKeyboard, (x, y) => x && y)
                .Subscribe(v => _keyboardInput.SetActive(v))
                .AddTo(this);
        }
        
        //TODO: receiverベースで切り替えるようになったらUpdateは不要
        private void Update()
        {
            _source.Value =
                sourceType == GameInputSourceType.GamePad ? _gamePadInput :
                sourceType == GameInputSourceType.Keyboard ? (IGameInputSource) _keyboardInput :
                EmptyGameInputSource.Instance;
            
            _gamePadInput.SetActive(_source.Value == _gamePadInput);
            _keyboardInput.SetActive(_source.Value == _keyboardInput);
        }
        
        
    }
}
