using UniRx;
using Zenject;

namespace Baku.VMagicMirror
{
    public sealed class KeyboardVisibilityUpdater : PresenterBase
    {
        [Inject]
        public KeyboardVisibilityUpdater(
            KeyboardVisibilityView view,
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
        
        private KeyboardVisibilityView _view;
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
                .Subscribe(_ => _view.SetVisibility(IsKeyboardVisible()))
                .AddTo(this);

            _bodyMotionModeController
                .KeyboardAndMouseMotionMode
                .Subscribe(mode => _view.SetRightHandMeshRendererActive(
                    mode is KeyboardAndMouseMotionModes.KeyboardAndTouchPad
                ))
                .AddTo(this);
        }

        private bool IsKeyboardVisible()
        {
            // 設定の組み合わせに基づいたvisibilityがオフならその時点で非表示にする。手下げモードのときも表示しない
            var settingBasedResult =
                _deviceVisibilityRepository.HidVisible.Value &&
                _bodyMotionModeController.MotionMode.Value is BodyMotionMode.Default &&
                _bodyMotionModeController.KeyboardAndMouseMotionMode.Value is not KeyboardAndMouseMotionModes.None;

            if (!settingBasedResult)
            {
                return false;
            }
            
            if (!_deviceVisibilityRepository.HideUnusedDevices.Value)
            {
                return true;
            }

            // NOTE: マウスパッド操作中は「キーマウ操作」という括りで考えてキーボードも表示する
            return
                _handIkIntegrator.LeftTargetType.Value is HandTargetType.Keyboard ||
                _handIkIntegrator.RightTargetType.Value is HandTargetType.Keyboard or HandTargetType.Mouse;
        }
    }
}
