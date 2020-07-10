namespace Baku.VMagicMirror
{
    public class WindSettingReceiver
    {
        public WindSettingReceiver(IMessageReceiver receiver, VRMWind wind)
        {
            receiver.AssignCommandHandler(
                MessageCommandNames.WindEnable,
                c => wind.EnableWind(c.ToBoolean())
                );
            receiver.AssignCommandHandler(
                MessageCommandNames.WindStrength,
                c => wind.SetStrength(c.ParseAsPercentage())
                );
            receiver.AssignCommandHandler(
                MessageCommandNames.WindInterval, 
                c => wind.SetInterval(c.ParseAsPercentage())
                );
            receiver.AssignCommandHandler(
                MessageCommandNames.WindYaw,
                c => wind.WindYawDegree = c.ToInt()
                );
        }
    }
}
