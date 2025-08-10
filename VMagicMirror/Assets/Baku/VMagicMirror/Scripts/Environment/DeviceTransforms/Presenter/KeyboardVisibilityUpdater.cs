using R3;
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
            // 設定の組み合わせに基づいたvisibilityがオフならその時点で非表示にする。ただし手下げモードのときは表示するほうに寄せておく
            var settingBasedResult =
                _deviceVisibilityRepository.HidVisible.CurrentValue &&
                _bodyMotionModeController.MotionMode.CurrentValue is BodyMotionMode.Default or BodyMotionMode.StandingOnly &&
                _bodyMotionModeController.KeyboardAndMouseMotionMode.CurrentValue is not KeyboardAndMouseMotionModes.None;

            if (!settingBasedResult)
            {
                return false;
            }
            
            if (!_deviceVisibilityRepository.HideUnusedDevices.CurrentValue)
            {
                return true;
            }

            // NOTE: マウスパッド操作中は「キーマウ操作」という括りで考えてキーボードも表示する
            // ペンタブやプレゼンテーションモードの右手はマウスの一種とは見なさない (このケースではキー入力が全部左手扱いになっているはず)
            return
                _bodyMotionModeController.MotionMode.CurrentValue is BodyMotionMode.Default && 
                _handIkIntegrator.LeftTargetType.CurrentValue is HandTargetType.Keyboard ||
                _handIkIntegrator.RightTargetType.CurrentValue is HandTargetType.Keyboard or HandTargetType.Mouse;
        }
    }
}
