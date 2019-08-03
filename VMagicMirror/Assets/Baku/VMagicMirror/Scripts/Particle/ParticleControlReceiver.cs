using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror
{
    [RequireComponent(typeof(ParticleStore))]
    public class ParticleControlReceiver : MonoBehaviour
    {
        [SerializeField]
        private ReceivedMessageHandler _handler = null;

        private ParticleStore _particleStore = null;

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
                    default:
                        break;
                }
            });
        }

        private void SetParticleType(int typeIndex)
        {
            _particleStore.SetParticleIndex(typeIndex);
        }

    }
}
