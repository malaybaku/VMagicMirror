using UnityEngine;
using UniRx;

namespace Baku.VMagicMirror
{
    public class LipSyncController : MonoBehaviour
    {
        [SerializeField]
        private ReceivedMessageHandler handler = null;

        private AnimMorphEasedTarget _animMorphTarget = null;

        private void Start()
        {
            _animMorphTarget = GetComponent<AnimMorphEasedTarget>();
            handler.Messages.Subscribe(message =>
            {
                if (message.Command == MessageCommandNames.EnableLipSync)
                {
                    SetLipSyncEnable(message.ToBoolean());
                }
            });
        }

        private void SetLipSyncEnable(bool isEnabled)
        {
            _animMorphTarget.enabled = isEnabled;
        }
    }
}
