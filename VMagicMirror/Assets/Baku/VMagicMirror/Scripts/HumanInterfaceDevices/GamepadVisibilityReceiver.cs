using UnityEngine;
using Zenject;
using UniRx;

namespace Baku.VMagicMirror
{
    public class GamepadVisibilityReceiver : MonoBehaviour
    {
        [Inject] private ReceivedMessageHandler _handler;

        private Renderer[] _renderers = new Renderer[0];

        private void Start()
        {
            _handler.Commands.Subscribe(message =>
            {
                switch (message.Command)
                {
                    case MessageCommandNames.GamepadVisibility:
                        SetGamepadVisibility(message.ToBoolean());
                        break;
                }
            });

            _renderers = GetComponentsInChildren<Renderer>();
        }

        private void SetGamepadVisibility(bool visible)
        {
            foreach (var r in _renderers)
            {
                r.enabled = visible;
            }
        }
    }
}
