using System;
using UnityEngine;
using UniRx;
using Zenject;

namespace Baku.VMagicMirror
{
    [RequireComponent(typeof(ShoulderRotationModifier))]
    public class ShoulderRotationModifierReceiver : MonoBehaviour
    {
        [Inject] private ReceivedMessageHandler _handler = null;

        private void Start()
        {
            var modifier = GetComponent<ShoulderRotationModifier>();
            _handler.Commands.Subscribe(c =>
            {
                if (c.Command == MessageCommandNames.EnableShoulderMotionModify)
                {
                    modifier.EnableRotationModification = c.ToBoolean();
                }
            });
        }
    }
}
