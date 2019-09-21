using UnityEngine;
using UniRx;

namespace Baku.VMagicMirror
{
    public class EyebrowBlendShapeAssignController : MonoBehaviour
    {
        [SerializeField] private ReceivedMessageHandler handler;

        [SerializeField] private GrpcSender sender;

        [SerializeField] private FaceControlManager faceControlManager;
        
        private void Start()
        {
            handler.Commands.Subscribe(message =>
            {
                switch (message.Command)
                {
                    case MessageCommandNames.EyebrowLeftUpKey:
                        faceControlManager.EyebrowBlendShape.LeftUpKey = message.Content;
                        faceControlManager.EyebrowBlendShape.RefreshTarget(
                            faceControlManager.BlendShapeStore
                            );
                        break;
                    case MessageCommandNames.EyebrowLeftDownKey:
                        faceControlManager.EyebrowBlendShape.LeftDownKey = message.Content;
                        faceControlManager.EyebrowBlendShape.RefreshTarget(
                            faceControlManager.BlendShapeStore
                        );
                        break;
                    case MessageCommandNames.UseSeparatedKeyForEyebrow:
                        faceControlManager.EyebrowBlendShape.UseSeparatedTarget = message.ToBoolean();
                        faceControlManager.EyebrowBlendShape.RefreshTarget(
                            faceControlManager.BlendShapeStore
                        );
                        break;
                    case MessageCommandNames.EyebrowRightUpKey:
                        faceControlManager.EyebrowBlendShape.RightUpKey = message.Content;
                        faceControlManager.EyebrowBlendShape.RefreshTarget(
                            faceControlManager.BlendShapeStore
                        );
                        break;
                    case MessageCommandNames.EyebrowRightDownKey:
                        faceControlManager.EyebrowBlendShape.RightDownKey = message.Content;
                        faceControlManager.EyebrowBlendShape.RefreshTarget(
                            faceControlManager.BlendShapeStore
                        );
                        break;
                    case MessageCommandNames.EyebrowUpScale:
                        faceControlManager.EyebrowBlendShape.UpScale = message.ParseAsPercentage();
                        break;
                    case MessageCommandNames.EyebrowDownScale:
                        faceControlManager.EyebrowBlendShape.DownScale = message.ParseAsPercentage();
                        break;
                }
            });
            
            handler.QueryRequested += OnQueryReceived;
        }

        public void SendBlendShapeNames()
        {
            sender.SendCommand(
                MessageFactory.Instance.SetBlendShapeNames(GetTabSeparatedBlendShapeNames())
                );
        }

        private void OnQueryReceived(ReceivedQuery query)
        {
            if (query.Command == MessageQueryNames.GetBlendShapeNames)
            {
                query.Result = GetTabSeparatedBlendShapeNames();
            }
        }

        private string GetTabSeparatedBlendShapeNames()
        {
            return string.Join(
                "\t", 
                faceControlManager.BlendShapeStore.GetBlendShapeNames()
                );
        }
    }
}