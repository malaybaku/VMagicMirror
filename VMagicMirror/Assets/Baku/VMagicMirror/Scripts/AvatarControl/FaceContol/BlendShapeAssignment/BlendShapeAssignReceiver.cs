namespace Baku.VMagicMirror
{
    public class BlendShapeAssignReceiver
    {
        public BlendShapeAssignReceiver(IMessageReceiver receiver, FaceControlManager faceControlManager)
        {
            _faceControlManager = faceControlManager;
            
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
        }
        
        private readonly FaceControlManager _faceControlManager;
        private EyebrowBlendShapeSet EyebrowBlendShape => _faceControlManager.EyebrowBlendShape;
        
        private void RefreshTarget() => EyebrowBlendShape.RefreshTarget(_faceControlManager.BlendShapeStore);
    }
}
