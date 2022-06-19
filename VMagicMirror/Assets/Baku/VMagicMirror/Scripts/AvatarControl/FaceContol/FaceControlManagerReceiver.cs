namespace Baku.VMagicMirror
{ 
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
