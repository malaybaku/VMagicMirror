namespace Baku.VMagicMirror
{
    public class FaceControlManagerMessageIo
    {
        public FaceControlManagerMessageIo(IMessageReceiver receiver, IMessageSender sender, FaceControlManager faceControlManager)
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
                       
            receiver.AssignQueryHandler(
                VmmQueries.GetBlendShapeNames,
                query => query.Result = string.Join("\t", faceControlManager.BlendShapeStore.GetBlendShapeNames())
            );

            faceControlManager.VrmInitialized += () =>
            {
                sender.SendCommand(MessageFactory.Instance.SetBlendShapeNames(
                    string.Join("\t", faceControlManager.BlendShapeStore.GetBlendShapeNames())
                ));
            };

            //特に眉まわりのブレンドシェイプ割り当てだけは別途やる
            var _ = new BlendShapeAssignReceiver(receiver, faceControlManager);
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
