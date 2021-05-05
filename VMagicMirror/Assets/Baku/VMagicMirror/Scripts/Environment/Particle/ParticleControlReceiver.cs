namespace Baku.VMagicMirror
{
    public class ParticleControlReceiver
    {
        private const int InvalidTypingEffectIndex = ParticleStore.InvalidTypingEffectIndex;

        public ParticleControlReceiver(IMessageReceiver receiver, ParticleStore particleStore)
        {
            _particleStore = particleStore;
            receiver.AssignCommandHandler(
                VmmCommands.SetKeyboardTypingEffectType,
                message => SetParticleType(message.ToInt())
                );
            receiver.AssignCommandHandler(
                VmmCommands.HidVisibility,
                message => SetKeyboardVisibility(message.ToBoolean())
                );
            receiver.AssignCommandHandler(
                VmmCommands.MidiControllerVisibility,
                message => SetMidiVisibility(message.ToBoolean())
                );
            receiver.AssignCommandHandler(
                VmmCommands.GamepadVisibility,
                v =>
                {
                    _gamepadDeviceVisible = v.ToBoolean();
                    UpdateParticleIndex();
                });
            receiver.AssignCommandHandler(
                VmmCommands.SetGamepadMotionMode, v =>
                {
                    _gamepadMotionMode = (GamepadMotionModes) v.ToInt();
                    UpdateParticleIndex();
                });
        }

        private readonly ParticleStore _particleStore = null;

        private int _selectedIndex = -1;
        private bool _keyboardIsVisible = true;
        private bool _midiVisible = false;

        private bool _gamepadDeviceVisible = false;
        private GamepadMotionModes _gamepadMotionMode = GamepadMotionModes.Gamepad;
        
        private void SetParticleType(int typeIndex)
        {
            _selectedIndex = typeIndex;
            UpdateParticleIndex();
        }

        private void SetKeyboardVisibility(bool visible)
        {
            _keyboardIsVisible = visible;
            UpdateParticleIndex();
        }

        private void SetMidiVisibility(bool visible)
        {
            _midiVisible = visible;
            UpdateParticleIndex();
        }

        private void UpdateParticleIndex()
        {
            _particleStore.SetParticleIndex(
                _keyboardIsVisible ? _selectedIndex : InvalidTypingEffectIndex,
                _midiVisible ? _selectedIndex : InvalidTypingEffectIndex,
                _gamepadDeviceVisible && _gamepadMotionMode == GamepadMotionModes.ArcadeStick 
                    ? _selectedIndex
                    : InvalidTypingEffectIndex
                );
        }

    }
}
