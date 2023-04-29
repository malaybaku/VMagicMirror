using Baku.VMagicMirror.GameInput;
using UniRx;
using Zenject;

namespace Baku.VMagicMirror
{
    public class HorizontalFlipController : PresenterBase
    {
        private readonly IMessageReceiver _receiver;
        private readonly BodyMotionModeController _bodyMotionModeController;
        
        private readonly ReactiveProperty<bool> _disableHorizontalFlip = new ReactiveProperty<bool>(false);
        public IReadOnlyReactiveProperty<bool> DisableHorizontalFlip => _disableHorizontalFlip;

        private readonly ReactiveProperty<bool> _uiOptionDisablesHorizontalFlip = new ReactiveProperty<bool>(false);

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
            _receiver.AssignCommandHandler(
                VmmCommands.DisableFaceTrackingHorizontalFlip,
                c => _uiOptionDisablesHorizontalFlip.Value = c.ToBoolean()
                );

            _uiOptionDisablesHorizontalFlip.CombineLatest(
                _bodyMotionModeController.MotionMode,
                _bodyMotionModeController.CurrentGameInputLocomotionStyle,
                (option, mode, gameInputLocomotion) => 
                    option ||
                    (mode == BodyMotionMode.GameInputLocomotion && 
                     IsThirdPersonLocomotionStyle(gameInputLocomotion))
                )
                .Subscribe(disable => _disableHorizontalFlip.Value = disable)
                .AddTo(this);
        }

        private static bool IsThirdPersonLocomotionStyle(GameInputLocomotionStyle style)
            => style != GameInputLocomotionStyle.FirstPerson;
    }
}
