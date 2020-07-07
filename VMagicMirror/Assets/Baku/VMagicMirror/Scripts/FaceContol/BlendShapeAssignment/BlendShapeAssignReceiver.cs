using Baku.VMagicMirror.InterProcess;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    public class BlendShapeAssignReceiver : MonoBehaviour
    {
        [SerializeField] private FaceControlManager faceControlManager = null;

        private EyebrowBlendShapeSet EyebrowBlendShape => faceControlManager.EyebrowBlendShape;
        private IMessageSender _sender;

        [Inject]
        public void Initialize(IMessageReceiver receiver, IMessageSender sender)
        {
            _sender = sender;

            receiver.AssignCommandHandler(
                MessageCommandNames.EyebrowLeftUpKey,
                message =>
                {
                    EyebrowBlendShape.LeftUpKey = message.Content;
                    RefreshTarget();
                });
            receiver.AssignCommandHandler(
                MessageCommandNames.EyebrowLeftDownKey,
                message =>
                {
                    EyebrowBlendShape.LeftDownKey = message.Content;
                    RefreshTarget();
                });
            receiver.AssignCommandHandler(
                MessageCommandNames.EyebrowLeftDownKey,
                message =>
                {
                    EyebrowBlendShape.LeftDownKey = message.Content;
                    RefreshTarget();
                });
            receiver.AssignCommandHandler(
                MessageCommandNames.UseSeparatedKeyForEyebrow,
                message =>
                {
                    EyebrowBlendShape.UseSeparatedTarget = message.ToBoolean();
                    RefreshTarget();
                });
            receiver.AssignCommandHandler(
                MessageCommandNames.EyebrowRightUpKey,
                message =>
                {
                    EyebrowBlendShape.RightUpKey = message.Content;
                    RefreshTarget();
                });
            receiver.AssignCommandHandler(
                MessageCommandNames.EyebrowRightDownKey,
                message =>
                {
                    EyebrowBlendShape.RightDownKey = message.Content;
                    RefreshTarget();
                });
            receiver.AssignCommandHandler(
                MessageCommandNames.EyebrowUpScale,
                message => EyebrowBlendShape.UpScale = message.ParseAsPercentage()
                );
            receiver.AssignCommandHandler(
                MessageCommandNames.EyebrowDownScale, 
                message => EyebrowBlendShape.DownScale = message.ParseAsPercentage()
                );
            
            receiver.AssignQueryHandler(
                MessageQueryNames.GetBlendShapeNames,
                query => query.Result = string.Join("\t", TryGetBlendShapeNames())
                );
            

            faceControlManager.VrmInitialized += SendBlendShapeNames;
        }
        
        private void RefreshTarget() => EyebrowBlendShape.RefreshTarget(faceControlManager.BlendShapeStore);

        public string[] TryGetBlendShapeNames() => faceControlManager.BlendShapeStore.GetBlendShapeNames();
        
        private void SendBlendShapeNames()
            => _sender.SendCommand(MessageFactory.Instance.SetBlendShapeNames(
                string.Join("\t", TryGetBlendShapeNames())
                ));
    }
}
