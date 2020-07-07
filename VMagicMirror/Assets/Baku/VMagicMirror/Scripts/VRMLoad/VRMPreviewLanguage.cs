using Baku.VMagicMirror.InterProcess;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    public class VRMPreviewLanguage : MonoBehaviour
    {
        //TODO: 非MonoBehaviour化
        public string Language { get; private set; } = "Japanese";

        [Inject]
        public void Initialize(IMessageReceiver receiver)
        {
            receiver.AssignCommandHandler(
                MessageCommandNames.Language,
                message => Language = message.Content
            );
        }
    }
}
