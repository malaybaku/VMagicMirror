using UniRx;
using Zenject;

namespace Baku.VMagicMirror
{
    public sealed class TouchpadVisibilityUpdater : PresenterBase
    {
        [Inject]
        public TouchpadVisibilityUpdater(
            TouchpadVisibilityView view,
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
        
        private TouchpadVisibilityView _view;
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
                _handIkIntegrator.LeftTargetType.AsUnitWithoutLatest(),
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
                _bodyMotionModeController.KeyboardAndMouseMotionMode.Value is KeyboardAndMouseMotionModes.KeyboardAndTouchPad;

            if (!settingBasedResult)
            {
                return false;
            }
            
            if (!_deviceVisibilityRepository.HideUnusedDevices.Value)
            {
                return true;
            }
            
            //NOTE: キーボードに左右どっちかの手が乗ってる場合、キーマウ操作の一環と見なして表示してもOKにする。
            // 特に右手のキーボードを許容しないとキーマウの行き来でデバイスが出入りしてうるさいので、その対策をしている
            return
                _bodyMotionModeController.MotionMode.Value is BodyMotionMode.Default && 
                _handIkIntegrator.LeftTargetType.Value is HandTargetType.Keyboard || 
                _handIkIntegrator.RightTargetType.Value is HandTargetType.Mouse or HandTargetType.Keyboard;
        }
    }
}
