using UnityEngine;
using Zenject;
using Baku.VMagicMirror.InterProcess;

namespace Baku.VMagicMirror
{
    [RequireComponent(typeof(ShoulderRotationModifier))]
    public class ShoulderRotationModifierReceiver : MonoBehaviour
    {
        //TODO: 非MonoBehaviour化
        private ShoulderRotationModifier _modifier = null;

        [Inject]
        public void Initialize(IMessageReceiver receiver)
        {
            receiver.AssignCommandHandler(
                MessageCommandNames.EnableShoulderMotionModify,
                command => _modifier.EnableRotationModification = command.ToBoolean()
                );
        }
        private void Start()
        {
            _modifier = GetComponent<ShoulderRotationModifier>();
        }
    }
}
