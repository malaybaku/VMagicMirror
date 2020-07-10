namespace Baku.VMagicMirror
{
    public class ElbowMotionModifyReceiver
    {
        public ElbowMotionModifyReceiver(IMessageReceiver receiver, ElbowMotionModifier modifier)
        {
            receiver.AssignCommandHandler(
                MessageCommandNames.SetWaistWidth,
                message => modifier.SetWaistWidth(message.ParseAsCentimeter())
                );
            receiver.AssignCommandHandler(
                MessageCommandNames.SetElbowCloseStrength,
                message => modifier.SetElbowCloseStrength(message.ParseAsPercentage())
            );
        }
    }
}
