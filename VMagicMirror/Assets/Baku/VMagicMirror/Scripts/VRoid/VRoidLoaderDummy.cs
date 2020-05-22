using UnityEngine;
using UniRx;
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
        private ReceivedMessageHandler _receiver = null;
        private IMessageSender _sender = null;
        
        [Inject]
        public void Initialize(ReceivedMessageHandler receiver, IMessageSender sender)
        {
            _receiver = receiver;
            _sender = sender;
        }

        private void Start()
        {
            _receiver.Commands.Subscribe(c =>
            {
                switch (c.Command)
                {
                    case MessageCommandNames.OpenVRoidSdkUi:
                        _sender?.SendCommand(MessageFactory.Instance.VRoidModelLoadCanceled());
                        break;
                    case MessageCommandNames.RequestLoadVRoidWithId:
                        _sender?.SendCommand(MessageFactory.Instance.VRoidModelLoadCanceled());
                        break;
                }
            });
        }
    }
}
