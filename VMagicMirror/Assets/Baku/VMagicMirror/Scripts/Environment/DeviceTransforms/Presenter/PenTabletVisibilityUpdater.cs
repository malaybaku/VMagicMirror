using R3;
using Zenject;

namespace Baku.VMagicMirror
{
    public sealed class PenTabletVisibilityUpdater : PresenterBase
    {
        [Inject]
        public PenTabletVisibilityUpdater(
            PenTabletVisibilityView view,
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
        
        private PenTabletVisibilityView _view;
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
            // 設定の組み合わせに基づいたvisibilityがオフならその時点で非表示にする。ただし手下げモードのときは表示するほうに寄せておく
            var settingBasedResult =
                _deviceVisibilityRepository.HidVisible.Value &&
                _bodyMotionModeController.MotionMode.Value is BodyMotionMode.Default or BodyMotionMode.StandingOnly &&
                _bodyMotionModeController.KeyboardAndMouseMotionMode.Value is KeyboardAndMouseMotionModes.PenTablet;

            if (!settingBasedResult)
            {
                return false;
            }
            
            if (!_deviceVisibilityRepository.HideUnusedDevices.Value)
            {
                return true;
            }
            
            return 
                _bodyMotionModeController.MotionMode.Value is BodyMotionMode.Default && 
                _handIkIntegrator.RightTargetType.Value is HandTargetType.PenTablet;
        }
    }
}
