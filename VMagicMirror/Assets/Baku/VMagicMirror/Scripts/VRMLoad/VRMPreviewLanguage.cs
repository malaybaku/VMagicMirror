using UnityEngine;
using UniRx;

namespace Baku.VMagicMirror
{
    public class VrmPreviewLanguage : MonoBehaviour
    {
        [SerializeField]
        private ReceivedMessageHandler handler;

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
