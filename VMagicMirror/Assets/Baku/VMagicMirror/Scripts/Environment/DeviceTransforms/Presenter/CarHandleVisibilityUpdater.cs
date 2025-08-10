using R3;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 車のハンドルオブジェクトのon/offを制御するやつ
    /// </summary>
    public sealed class CarHandleVisibilityUpdater : PresenterBase
    {
        [Inject]
        public CarHandleVisibilityUpdater(
            CarHandleVisibilityView view,
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

        private CarHandleVisibilityView _view;
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
                .Subscribe(_ => _view.SetVisible(IsCarHandleVisible()))
                .AddTo(this);
        }

        private bool IsCarHandleVisible()
        {
            // 設定の組み合わせに基づいたvisibilityがオフならその時点で非表示にしておく
            var settingBasedResult =
                _deviceVisibilityRepository.GamepadVisible.CurrentValue &&
                _bodyMotionModeController.MotionMode.CurrentValue is BodyMotionMode.Default or BodyMotionMode.StandingOnly &&
                _bodyMotionModeController.GamepadMotionMode.CurrentValue is GamepadMotionModes.CarController;

            if (!settingBasedResult)
            {
                return false;
            }

            if (!_deviceVisibilityRepository.HideUnusedDevices.CurrentValue)
            {
                return true;
            }

            // この行まで到達した場合、設定に加えて動的な手IKの状態も考慮して表示/非表示を決める
            return
                _bodyMotionModeController.MotionMode.CurrentValue is BodyMotionMode.Default && 
                _handIkIntegrator.LeftTargetType.CurrentValue is HandTargetType.CarHandle ||
                _handIkIntegrator.RightTargetType.CurrentValue is HandTargetType.CarHandle;
        }
    }
}
