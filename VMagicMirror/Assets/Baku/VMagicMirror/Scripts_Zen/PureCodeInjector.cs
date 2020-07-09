using Baku.VMagicMirror.InterProcess;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// Non-MonoBehaviourなクラスで依存元にぶら下がって終わり、のやつを作ってくれるクラス
    /// </summary>
    public class PureCodeInjector : MonoBehaviour
    {
        [Inject]
        public void Initialize(IMessageReceiver receiver)
        {
            var _ = new ImageQualitySettingReceiver(receiver);
        }
    }
}
