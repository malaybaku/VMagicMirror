namespace Baku.VMagicMirror
{
    public class WindSettingReceiver
    {
        public WindSettingReceiver(IMessageReceiver receiver, VRMWind wind)
        {
            receiver.AssignCommandHandler(
                VmmCommands.WindEnable,
                c => wind.EnableWind(c.ToBoolean())
                );
            receiver.AssignCommandHandler(
                VmmCommands.WindStrength,
                c => wind.SetStrength(c.ParseAsPercentage())
                );
            receiver.AssignCommandHandler(
                VmmCommands.WindInterval, 
                c => wind.SetInterval(c.ParseAsPercentage())
                );
            receiver.AssignCommandHandler(
                VmmCommands.WindYaw,
                c => wind.WindYawDegree = c.ToInt()
                );
        }
    }
}
