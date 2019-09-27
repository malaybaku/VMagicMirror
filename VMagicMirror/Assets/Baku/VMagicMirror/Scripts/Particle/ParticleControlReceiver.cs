using UniRx;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    [RequireComponent(typeof(ParticleStore))]
    public class ParticleControlReceiver : MonoBehaviour
    {
        private const int InvalidTypingEffectIndex = ParticleStore.InvalidTypingEffectIndex;

        [Inject] private ReceivedMessageHandler _handler = null;

        private ParticleStore _particleStore = null;

        private bool _keyboardIsVisible = true;
        private int _selectedIndex = -1;

        void Start()
        {
            _particleStore = GetComponent<ParticleStore>();

            _handler.Commands.Subscribe(message =>
            {
                switch (message.Command)
                {
                    case MessageCommandNames.SetKeyboardTypingEffectType:
                        SetParticleType(message.ToInt());
                        break;
                    case MessageCommandNames.HidVisibility:
                        SetKeyboardVisibility(message.ToBoolean());
                        break;
                    default:
                        break;
                }
            });
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

        private void UpdateParticleIndex()
        {
            _particleStore.SetParticleIndex(
                _keyboardIsVisible ? _selectedIndex : InvalidTypingEffectIndex
                );
        }

    }
}
