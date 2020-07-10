namespace Baku.VMagicMirror
{
    public class BlendShapeAssignReceiver
    {
        public BlendShapeAssignReceiver(IMessageReceiver receiver, FaceControlManager faceControlManager)
        {
            _faceControlManager = faceControlManager;
            
            receiver.AssignCommandHandler(
                VmmCommands.EyebrowLeftUpKey,
                message =>
                {
                    EyebrowBlendShape.LeftUpKey = message.Content;
                    RefreshTarget();
                });
            receiver.AssignCommandHandler(
                VmmCommands.EyebrowLeftDownKey,
                message =>
                {
                    EyebrowBlendShape.LeftDownKey = message.Content;
                    RefreshTarget();
                });
            receiver.AssignCommandHandler(
                VmmCommands.EyebrowLeftDownKey,
                message =>
                {
                    EyebrowBlendShape.LeftDownKey = message.Content;
                    RefreshTarget();
                });
            receiver.AssignCommandHandler(
                VmmCommands.UseSeparatedKeyForEyebrow,
                message =>
                {
                    EyebrowBlendShape.UseSeparatedTarget = message.ToBoolean();
                    RefreshTarget();
                });
            receiver.AssignCommandHandler(
                VmmCommands.EyebrowRightUpKey,
                message =>
                {
                    EyebrowBlendShape.RightUpKey = message.Content;
                    RefreshTarget();
                });
            receiver.AssignCommandHandler(
                VmmCommands.EyebrowRightDownKey,
                message =>
                {
                    EyebrowBlendShape.RightDownKey = message.Content;
                    RefreshTarget();
                });
            receiver.AssignCommandHandler(
                VmmCommands.EyebrowUpScale,
                message => EyebrowBlendShape.UpScale = message.ParseAsPercentage()
                );
            receiver.AssignCommandHandler(
                VmmCommands.EyebrowDownScale, 
                message => EyebrowBlendShape.DownScale = message.ParseAsPercentage()
                );
        }
        
        private readonly FaceControlManager _faceControlManager;
        private EyebrowBlendShapeSet EyebrowBlendShape => _faceControlManager.EyebrowBlendShape;
        
        private void RefreshTarget() => EyebrowBlendShape.RefreshTarget(_faceControlManager.BlendShapeStore);
    }
}
