using Baku.VMagicMirror.InterProcess;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    public class ElbowMotionModifyReceiver : MonoBehaviour
    {
        //TODO: 非MonoBehaviour化
        [SerializeField] private ElbowMotionModifier modifier = null;

        [Inject]
        public void Initialize(IMessageReceiver receiver)
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
