using UnityEngine;
using UniRx;

namespace Baku.VMagicMirror
{
    public class ElbowMotionModifyReceiver : MonoBehaviour
    {
        [SerializeField]
        private ReceivedMessageHandler handler = null;

        public float WaistWidthHalf { get; private set; } = 0.15f;
        public float ElbowCloseStrength { get; private set; } = 0.30f;

        private void Start()
        {
            handler.Commands.Subscribe(message =>
            {
                switch (message.Command)
                {
                    case MessageCommandNames.SetWaistWidth:
                        SetWaistWidth(message.ParseAsCentimeter());
                        break;
                    case MessageCommandNames.SetElbowCloseStrength:
                        SetElbowCloseStrength(message.ParseAsPercentage());
                        break;
                }
            });
        }

        private void SetWaistWidth(float width) => WaistWidthHalf = width * 0.5f;
        private void SetElbowCloseStrength(float strength) => ElbowCloseStrength = strength;
    }
}
