using UnityEngine;
using Zenject;
using Baku.VMagicMirror.InterProcess;

namespace Baku.VMagicMirror
{
    public class ParticleControlReceiver : MonoBehaviour
    {
        //TODO: 非MonoBehaviour化
        private const int InvalidTypingEffectIndex = ParticleStore.InvalidTypingEffectIndex;

        private ParticleStore _particleStore = null;

        private int _selectedIndex = -1;
        private bool _keyboardIsVisible = true;
        private bool _midiVisible = false;

        [Inject]
        public void Initialize(IMessageReceiver receiver, ParticleStore particleStore)
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
