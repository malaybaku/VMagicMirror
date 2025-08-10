using Baku.VMagicMirror.GameInput;
using R3;
using Zenject;

namespace Baku.VMagicMirror
{
    public class HorizontalFlipController : PresenterBase
    {
        private readonly IMessageReceiver _receiver;
        private readonly BodyMotionModeController _bodyMotionModeController;
        
        private readonly ReactiveProperty<bool> _disableFaceHorizontalFlip = new(false);
        public IReadOnlyReactiveProperty<bool> DisableFaceHorizontalFlip => _disableFaceHorizontalFlip;
        
        private readonly ReactiveProperty<bool> _disableHandHorizontalFlip = new(false);
        public IReadOnlyReactiveProperty<bool> DisableHandHorizontalFlip => _disableHandHorizontalFlip;

        // NOTE: 書いてる通り、UI上では手以外と手の反転オプションが独立の存在するので、そこは注意。
        private readonly ReactiveProperty<bool> _uiOptionDisablesFaceHorizontalFlip = new(false);
        private readonly ReactiveProperty<bool> _uiOptionDisablesHandHorizontalFlip = new(false);

        [Inject]
        public HorizontalFlipController(
            IMessageReceiver receiver, 
            BodyMotionModeController bodyMotionModeController
            )
        {
            _receiver = receiver;
            _bodyMotionModeController = bodyMotionModeController;
        }

        public override void Initialize()
        {
            _receiver.BindBoolProperty(
                VmmCommands.DisableFaceTrackingHorizontalFlip,
                _uiOptionDisablesFaceHorizontalFlip
            );

            _receiver.BindBoolProperty(
                VmmCommands.DisableHandTrackingHorizontalFlip,
                _uiOptionDisablesHandHorizontalFlip
            );
            
            _uiOptionDisablesFaceHorizontalFlip.CombineLatest(
                _bodyMotionModeController.MotionMode,
                _bodyMotionModeController.CurrentGameInputLocomotionStyle,
                (option, mode, gameInputLocomotion) => 
                    option ||
                    (mode == BodyMotionMode.GameInputLocomotion && 
                     IsThirdPersonLocomotionStyle(gameInputLocomotion))
                )
                .Subscribe(disable => _disableFaceHorizontalFlip.Value = disable)
                .AddTo(this);
            
            
            _uiOptionDisablesHandHorizontalFlip.CombineLatest(
                _bodyMotionModeController.MotionMode,
                _bodyMotionModeController.CurrentGameInputLocomotionStyle,
                (option, mode, gameInputLocomotion) => 
                    option ||
                    (mode == BodyMotionMode.GameInputLocomotion && 
                     IsThirdPersonLocomotionStyle(gameInputLocomotion))
                )
                .Subscribe(disable => _disableHandHorizontalFlip.Value = disable)
                .AddTo(this);
        }

        private static bool IsThirdPersonLocomotionStyle(GameInputLocomotionStyle style)
            => style != GameInputLocomotionStyle.FirstPerson;
    }
}
