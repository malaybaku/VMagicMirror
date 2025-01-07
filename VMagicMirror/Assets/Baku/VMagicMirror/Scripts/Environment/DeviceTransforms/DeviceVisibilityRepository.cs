using UniRx;
using Zenject;

namespace Baku.VMagicMirror
{
    public class DeviceVisibilityRepository : PresenterBase
    {
        private readonly IMessageReceiver _receiver;

        [Inject]
        public DeviceVisibilityRepository(IMessageReceiver receiver)
        {
            _receiver = receiver;
        }

        public override void Initialize()
        {
            _receiver.BindBoolProperty(VmmCommands.HidVisibility, _hidVisible);
            _receiver.BindBoolProperty(VmmCommands.SetPenVisibility, _penVisible);
            _receiver.BindBoolProperty(VmmCommands.GamepadVisibility, _gamepadVisible);
            _receiver.BindBoolProperty(VmmCommands.MidiControllerVisibility, _midiControllerVisible);
            _receiver.BindBoolProperty(VmmCommands.EnableDeviceFreeLayout, _enableDeviceFreeLayout);
            _receiver.BindBoolProperty(VmmCommands.HideUnusedDevices, _rawHideUnusedDevices);

            //NOTE: レイアウト編集中は自動でデバイスを隠すと編集に支障が出るので、隠さない方向に寄せる
            _rawHideUnusedDevices.CombineLatest(
                _enableDeviceFreeLayout,
                (hide, freeLayoutEnabled) => (hide && !freeLayoutEnabled)
                )
                .Subscribe(value => _hideUnusedDevices.Value = value)
                .AddTo(this);
        }

        private readonly ReactiveProperty<bool> _hidVisible = new(true);
        private readonly ReactiveProperty<bool> _penVisible = new(true);
        private readonly ReactiveProperty<bool> _gamepadVisible = new();
        private readonly ReactiveProperty<bool> _midiControllerVisible = new();

        private readonly ReactiveProperty<bool> _enableDeviceFreeLayout = new();
        private readonly ReactiveProperty<bool> _rawHideUnusedDevices = new();
        private readonly ReactiveProperty<bool> _hideUnusedDevices = new();
        
        public IReadOnlyReactiveProperty<bool> HidVisible => _hidVisible;
        public IReadOnlyReactiveProperty<bool> PenVisible => _penVisible;
        public IReadOnlyReactiveProperty<bool> GamepadVisible => _gamepadVisible;
        public IReadOnlyReactiveProperty<bool> MidiControllerVisible => _midiControllerVisible;
        public IReadOnlyReactiveProperty<bool> HideUnusedDevices => _hideUnusedDevices;
    }
}
