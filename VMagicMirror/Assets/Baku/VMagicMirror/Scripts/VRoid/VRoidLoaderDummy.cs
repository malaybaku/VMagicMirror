using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// VRoidHubへの接続リクエストが来たら即座にキャンセルで返すダミー。
    /// </summary>
    /// <remarks>
    /// VRoidSDKは公開物ではないため、接続コードを分離するためにダミー実装を用意しています。
    /// </remarks>
    public class VRoidLoaderDummy : MonoBehaviour
    {
        private IMessageSender _sender = null;
        
        [Inject]
        public void Initialize(IMessageReceiver receiver, IMessageSender sender)
        {
            _sender = sender;
            receiver.AssignCommandHandler(
                VmmCommands.OpenVRoidSdkUi,
                _ => _sender?.SendCommand(MessageFactory.VRoidModelLoadCanceled())
                );
            receiver.AssignCommandHandler(
                VmmCommands.RequestLoadVRoidWithId,
                _ => _sender?.SendCommand(MessageFactory.VRoidModelLoadCanceled())
                );
        }
    }
}
