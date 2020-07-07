using UnityEngine;
using Zenject;
using Baku.VMagicMirror.InterProcess;

namespace Baku.VMagicMirror
{
    public class WindSettingReceiver : MonoBehaviour
    {
        //TODO: 非MonoBehaviour化

        [SerializeField] private VRMWind wind = null;

        [Inject]
        public void Initialize(IMessageReceiver receiver)
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
