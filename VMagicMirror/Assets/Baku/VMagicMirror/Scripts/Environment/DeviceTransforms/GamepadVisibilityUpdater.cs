using UniRx;
using Zenject;

namespace Baku.VMagicMirror
{
    public sealed class GamepadVisibilityUpdater : PresenterBase
    {
        [Inject]
        public GamepadVisibilityUpdater(
            GamepadVisibilityView view,
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
        
        private GamepadVisibilityView _view;
        private DeviceVisibilityRepository _deviceVisibilityRepository;
        private BodyMotionModeController _bodyMotionModeController;
        private HandIKIntegrator _handIkIntegrator;
        private DeformableCounter _deformableCounter;

        public override void Initialize()
        {
            _view.Setup(_deformableCounter);

            //NOTE: 初期値で1回だけ発火してほしいので最初だけAsUnitObservableになっている
            Observable.Merge(
                _deviceVisibilityRepository.GamepadVisible.AsUnitObservable(),
                _deviceVisibilityRepository.HideUnusedDevices.AsUnitWithoutLatest(),
                _bodyMotionModeController.MotionMode.AsUnitWithoutLatest(),
                _bodyMotionModeController.GamepadMotionMode.AsUnitWithoutLatest(),
                _handIkIntegrator.LeftTargetType.AsUnitWithoutLatest(),
                _handIkIntegrator.RightTargetType.AsUnitWithoutLatest()
                )
                .Subscribe(_ => _view.SetVisible(IsGamepadVisible()))
                .AddTo(this);
        }

        private bool IsGamepadVisible()
        {
            // 設定の組み合わせに基づいたvisibilityがオフならその時点で非表示にしておく
            var settingBasedResult = 
                _deviceVisibilityRepository.GamepadVisible.Value &&
                _bodyMotionModeController.MotionMode.Value is BodyMotionMode.Default &&
                _bodyMotionModeController.GamepadMotionMode.Value is GamepadMotionModes.Gamepad;

            if (!settingBasedResult)
            {
                return false;
            }

            if (!_deviceVisibilityRepository.HideUnusedDevices.Value)
            {
                return true;
            }

            // この行まで到達した場合、設定に加えて動的な手IKの状態も考慮して表示/非表示を決める
            return
                _handIkIntegrator.LeftTargetType.Value is HandTargetType.Gamepad ||
                _handIkIntegrator.RightTargetType.Value is HandTargetType.Gamepad;
        }
    }
}
