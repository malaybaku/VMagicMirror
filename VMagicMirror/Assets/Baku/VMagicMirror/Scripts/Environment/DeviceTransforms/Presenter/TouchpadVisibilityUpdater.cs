using UniRx;
using Zenject;

namespace Baku.VMagicMirror
{
    public sealed class TouchpadVisibilityUpdater : PresenterBase
    {
        [Inject]
        public TouchpadVisibilityUpdater(
            TouchpadVisibility view,
            DeviceVisibilityRepository deviceVisibilityRepository,
            BodyMotionModeController bodyMotionModeController,
            HandIKIntegrator handIKIntegrator,
            DeformableCounter deformableCounter)
        {
            _view = view;
            _deviceVisibilityRepository = deviceVisibilityRepository;
            _bodyMotionModeController = bodyMotionModeController;
            _handIkIntegrator = handIKIntegrator;
            _deformableCounter = deformableCounter;
        }
        
        private TouchpadVisibility _view;
        private DeviceVisibilityRepository _deviceVisibilityRepository;
        private BodyMotionModeController _bodyMotionModeController;
        private HandIKIntegrator _handIkIntegrator;
        private DeformableCounter _deformableCounter;

        public override void Initialize()
        {
            _view.Setup(_deformableCounter);

            //NOTE: 初期値で1回だけ発火してほしいので最初だけAsUnitObservableになっている
            Observable.Merge(
                _deviceVisibilityRepository.HidVisible.AsUnitObservable(),
                _deviceVisibilityRepository.HideUnusedDevices.AsUnitWithoutLatest(),
                _bodyMotionModeController.MotionMode.AsUnitWithoutLatest(),
                _bodyMotionModeController.KeyboardAndMouseMotionMode.AsUnitWithoutLatest(),
                _handIkIntegrator.RightTargetType.AsUnitWithoutLatest()
                )
                .Subscribe(_ => _view.SetVisibility(IsTouchpadVisible()))
                .AddTo(this);
        }

        private bool IsTouchpadVisible()
        {
            // 設定の組み合わせに基づいたvisibilityがオフならその時点で非表示にする。手下げモードのときも表示しない
            var settingBasedResult =
                _deviceVisibilityRepository.HidVisible.Value &&
                _bodyMotionModeController.MotionMode.Value is BodyMotionMode.Default &&
                _bodyMotionModeController.KeyboardAndMouseMotionMode.Value is KeyboardAndMouseMotionModes.KeyboardAndTouchPad;

            if (!settingBasedResult)
            {
                return false;
            }
            
            if (!_deviceVisibilityRepository.HideUnusedDevices.Value)
            {
                return true;
            }
            
            return
                _handIkIntegrator.RightTargetType.Value is HandTargetType.Mouse;
        }
    }
}
