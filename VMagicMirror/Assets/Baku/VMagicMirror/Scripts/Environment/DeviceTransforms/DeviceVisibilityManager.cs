using UniRx;
using Zenject;

namespace Baku.VMagicMirror
{
    public class DeviceVisibilityManager : PresenterBase
    {
        private readonly IMessageReceiver _receiver;

        [Inject]
        public DeviceVisibilityManager(IMessageReceiver receiver)
        {
            _receiver = receiver;
        }

        public override void Initialize()
        {
            _receiver.BindBoolProperty(VmmCommands.HidVisibility, _hidVisible);
            _receiver.BindBoolProperty(VmmCommands.SetPenVisibility, _penVisible);
            _receiver.BindBoolProperty(VmmCommands.GamepadVisibility, _gamepadVisible);
            _receiver.BindBoolProperty(VmmCommands.MidiControllerVisibility, _midiControllerVisible);
        }

        private readonly ReactiveProperty<bool> _hidVisible = new(true);
        private readonly ReactiveProperty<bool> _penVisible = new(true);
        private readonly ReactiveProperty<bool> _gamepadVisible = new();
        private readonly ReactiveProperty<bool> _midiControllerVisible = new();

        public IReadOnlyReactiveProperty<bool> HidVisible => _hidVisible;
        public IReadOnlyReactiveProperty<bool> PenVisible => _penVisible;
        public IReadOnlyReactiveProperty<bool> GamepadVisible => _gamepadVisible;
        public IReadOnlyReactiveProperty<bool> MidiControllerVisible => _midiControllerVisible;
    }
}
