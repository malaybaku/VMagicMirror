using UnityEngine;
using Zenject;
using UniRx;

namespace Baku.VMagicMirror
{
    public class WindSettingReceiver : MonoBehaviour
    {
        [Inject] private ReceivedMessageHandler _handler = null;

        [SerializeField] private VRMWind wind = null;

        private void Start()
        {
            _handler.Commands.Subscribe(c =>
            {
                switch (c.Command)
                {
                    case MessageCommandNames.WindEnable:
                        wind.EnableWind(c.ToBoolean());
                        break;
                    case MessageCommandNames.WindStrength:
                        wind.SetStrength(c.ParseAsPercentage());
                        break;
                    case MessageCommandNames.WindInterval:
                        wind.SetIntercal(c.ParseAsPercentage());
                        break;
                    case MessageCommandNames.WindYaw:
                        wind.WindYawDegree = c.ToInt();
                        break;
                }
            });
        }
    }
}
