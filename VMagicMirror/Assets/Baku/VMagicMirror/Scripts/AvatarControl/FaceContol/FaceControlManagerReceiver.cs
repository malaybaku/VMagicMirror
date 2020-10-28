namespace Baku.VMagicMirror
{
    public class FaceControlManagerMessageIo
    {
        public FaceControlManagerMessageIo(
            IMessageReceiver receiver, IMessageSender sender, 
            EyeBonePostProcess eyeBonePostProcess,
            FaceControlManager faceControlManager
            )
        {
            receiver.AssignCommandHandler(
                VmmCommands.AutoBlinkDuringFaceTracking,
                message => 
                    faceControlManager.PreferAutoBlinkOnWebCamTracking = message.ToBoolean()
                );

            receiver.AssignCommandHandler(
                VmmCommands.FaceDefaultFun,
                message =>
                    faceControlManager.DefaultBlendShape.FaceDefaultFunValue = message.ParseAsPercentage()
                );

            receiver.AssignCommandHandler(
                VmmCommands.SetEyeBoneRotationScale,
                message => eyeBonePostProcess.Scale = message.ParseAsPercentage()
            );
        }
    }

    public class BehaviorBasedBlinkReceiver
    {
        public BehaviorBasedBlinkReceiver(IMessageReceiver receiver, BehaviorBasedAutoBlinkAdjust autoBlinkAdjust)
        {
            receiver.AssignCommandHandler(
                VmmCommands.EnableHeadRotationBasedBlinkAdjust,
                message 
                    => autoBlinkAdjust.EnableHeadRotationBasedBlinkAdjust = message.ToBoolean()
            );
            receiver.AssignCommandHandler(
                VmmCommands.EnableLipSyncBasedBlinkAdjust,
                message =>
                    autoBlinkAdjust.EnableLipSyncBasedBlinkAdjust = message.ToBoolean()
            );
        }
    }
}
