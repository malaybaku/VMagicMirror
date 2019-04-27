using System;
using UnityEngine;
using UniRx;

namespace Baku.VMagicMirror
{
    public class MotionModifyReceiver : MonoBehaviour
    {
        public float WaistWidthHalf { get; private set; } = 0.15f;
        public float ElbowCloseStrength { get; private set; } = 0.30f;

        private IDisposable _observer = null;
        private void OnDestroy() => _observer?.Dispose();

        public void SetHandler(ReceivedMessageHandler handler)
        {
            _observer?.Dispose();
            _observer = handler.Commands.Subscribe(message =>
            {
                switch (message.Command)
                {
                    case MessageCommandNames.SetWaistWidth:
                        SetWaistWidth(message.ParseAsCentimeter());
                        break;
                    case MessageCommandNames.SetElbowCloseStrength:
                        SetElbowCloseStrength(message.ParseAsPercentage());
                        break;
                    default:
                        break;
                }
            });
        }

        private void SetWaistWidth(float width) => WaistWidthHalf = width * 0.5f;
        private void SetElbowCloseStrength(float strength) => ElbowCloseStrength = strength;
    }
}
