using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    public class EnvSettingsInjector : MonoBehaviour
    {
        [Inject]
        public void Initialize(IMessageReceiver receiver)
        {
            var _ = new ImageQualitySettingReceiver(receiver);
        }
    }
}