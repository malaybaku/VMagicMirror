using UnityEngine;
using UniRx;
using Zenject;

namespace Baku.VMagicMirror
{
    public class VrmPreviewLanguage : MonoBehaviour
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
