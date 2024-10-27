using UniRx;
using Zenject;

namespace Baku.VMagicMirror
{
    public sealed class GamepadVisibilityUpdater : PresenterBase
    {
        [Inject]
        public GamepadVisibilityUpdater(
            GamepadVisibilityView view,
            DeviceVisibilityManager deviceVisibilityManager,
            BodyMotionModeController bodyMotionModeController,
            HandIKIntegrator handIKIntegrator,
            DeformableCounter deformableCounter)
        {
            _view = view;
            _deviceVisibilityManager = deviceVisibilityManager;
            _bodyMotionModeController = bodyMotionModeController;
            _handIkIntegrator = handIKIntegrator;
            _deformableCounter = deformableCounter;
        }
        
        private GamepadVisibilityView _view;
        private DeviceVisibilityManager _deviceVisibilityManager;
        private BodyMotionModeController _bodyMotionModeController;
        private HandIKIntegrator _handIkIntegrator;
        private DeformableCounter _deformableCounter;

        public bool IsVisible { get; private set; }

        public override void Initialize()
        {
            _view.Setup(_deformableCounter);

            //NOTE: 初期値で1回だけ発火してほしいので最初だけAsUnitObservableになっている
            Observable.Merge(
                _deviceVisibilityManager.GamepadVisible.AsUnitObservable(),
                _deviceVisibilityManager.HideUnusedDevices.AsUnitWithoutLatest(),
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
                _deviceVisibilityManager.GamepadVisible.Value &&
                _bodyMotionModeController.MotionMode.Value is BodyMotionMode.Default &&
                _bodyMotionModeController.GamepadMotionMode.Value is GamepadMotionModes.Gamepad;

            if (!settingBasedResult)
            {
                return false;
            }

            if (!_deviceVisibilityManager.HideUnusedDevices.Value)
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
