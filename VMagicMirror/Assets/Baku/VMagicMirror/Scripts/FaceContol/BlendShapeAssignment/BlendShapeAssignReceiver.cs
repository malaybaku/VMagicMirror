using UnityEngine;
using UniRx;
using Zenject;

namespace Baku.VMagicMirror
{
    public class BlendShapeAssignReceiver : MonoBehaviour
    {
        [Inject] private ReceivedMessageHandler handler = null;
        [Inject] private IMessageSender sender = null;
        [SerializeField] private FaceControlManager faceControlManager = null;

        private EyebrowBlendShapeSet EyebrowBlendShape => faceControlManager.EyebrowBlendShape;
        
        private void Start()
        {
            handler.Commands.Subscribe(message =>
            {
                switch (message.Command)
                {
                    case MessageCommandNames.EyebrowLeftUpKey:
                        EyebrowBlendShape.LeftUpKey = message.Content;
                        break;
                    case MessageCommandNames.EyebrowLeftDownKey:
                        EyebrowBlendShape.LeftDownKey = message.Content;
                        RefreshTarget();
                        break;
                    case MessageCommandNames.UseSeparatedKeyForEyebrow:
                        EyebrowBlendShape.UseSeparatedTarget = message.ToBoolean();
                        RefreshTarget();
                        break;
                    case MessageCommandNames.EyebrowRightUpKey:
                        EyebrowBlendShape.RightUpKey = message.Content;
                        RefreshTarget();
                        break;
                    case MessageCommandNames.EyebrowRightDownKey:
                        EyebrowBlendShape.RightDownKey = message.Content;
                        RefreshTarget();
                        break;
                    case MessageCommandNames.EyebrowUpScale:
                        EyebrowBlendShape.UpScale = message.ParseAsPercentage();
                        break;
                    case MessageCommandNames.EyebrowDownScale:
                        EyebrowBlendShape.DownScale = message.ParseAsPercentage();
                        break;
                }
            });
            handler.QueryRequested += OnQueryReceived;
            faceControlManager.VrmInitialized += SendBlendShapeNames;
        }
        
        private void RefreshTarget() => EyebrowBlendShape.RefreshTarget(faceControlManager.BlendShapeStore);

        public string[] TryGetBlendShapeNames() => faceControlManager.BlendShapeStore.GetBlendShapeNames();
        
        private void SendBlendShapeNames()
            => sender.SendCommand(MessageFactory.Instance.SetBlendShapeNames(
                string.Join("\t", TryGetBlendShapeNames())
                ));

        private void OnQueryReceived(ReceivedQuery query)
        {
            if (query.Command == MessageQueryNames.GetBlendShapeNames)
            {
                query.Result = string.Join("\t", TryGetBlendShapeNames());
            }
        }
    }
}
