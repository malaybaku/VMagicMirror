namespace Baku.VMagicMirror
{
    public class ElbowMotionModifyReceiver
    {
        public ElbowMotionModifyReceiver(IMessageReceiver receiver, ElbowMotionModifier modifier)
        {
            receiver.AssignCommandHandler(
                VmmCommands.SetWaistWidth,
                message => modifier.SetWaistWidth(message.ParseAsCentimeter())
                );
            receiver.AssignCommandHandler(
                VmmCommands.SetElbowCloseStrength,
                message => modifier.SetElbowCloseStrength(message.ParseAsPercentage())
            );
        }
    }
}
