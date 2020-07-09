namespace Baku.VMagicMirror
{
    public class ParticleControlReceiver
    {
        private const int InvalidTypingEffectIndex = ParticleStore.InvalidTypingEffectIndex;

        public ParticleControlReceiver(IMessageReceiver receiver, ParticleStore particleStore)
        {
            _particleStore = particleStore;
            receiver.AssignCommandHandler(
                MessageCommandNames.SetKeyboardTypingEffectType,
                message => SetParticleType(message.ToInt())
                );
            receiver.AssignCommandHandler(
                MessageCommandNames.HidVisibility,
                message => SetKeyboardVisibility(message.ToBoolean())
                );
            receiver.AssignCommandHandler(
                MessageCommandNames.MidiControllerVisibility,
                message => SetMidiVisibility(message.ToBoolean())
                );
        }

        private readonly ParticleStore _particleStore = null;

        private int _selectedIndex = -1;
        private bool _keyboardIsVisible = true;
        private bool _midiVisible = false;
        
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
                _midiVisible ? _selectedIndex : InvalidTypingEffectIndex
                );
        }

    }
}
