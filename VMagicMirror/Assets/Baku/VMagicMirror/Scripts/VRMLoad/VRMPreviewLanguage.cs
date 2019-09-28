using UnityEngine;
using UniRx;
using Zenject;

namespace Baku.VMagicMirror
{
    public class VRMPreviewLanguage : MonoBehaviour
    {
        [Inject] private ReceivedMessageHandler handler = null;

        public string Language { get; private set; } = "Japanese";

        private void Start()
        {
            handler.Commands.Subscribe(message =>
            {
                if (message.Command == MessageCommandNames.Language)
                {
                    Language = message.Content;
                }
            });
        }
    }
}
