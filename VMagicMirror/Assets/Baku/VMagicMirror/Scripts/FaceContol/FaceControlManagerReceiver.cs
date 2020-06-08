using UnityEngine;
using UniRx;
using Zenject;

namespace Baku.VMagicMirror
{
    [RequireComponent(typeof(FaceControlManager))]
    [RequireComponent(typeof(BehaviorBasedAutoBlinkAdjust))]
    public class FaceControlManagerReceiver : MonoBehaviour
    {
        [Inject]
        public void Initialize(ReceivedMessageHandler handler) => _handler = handler;
        private ReceivedMessageHandler _handler = null;

        private FaceControlManager _faceControlManager;
        private BehaviorBasedAutoBlinkAdjust _autoBlinkAdjust;

        private void Start()
        {
            _faceControlManager = GetComponent<FaceControlManager>();
            _autoBlinkAdjust = GetComponent<BehaviorBasedAutoBlinkAdjust>();
            _handler.Commands.Subscribe(message =>
            {
                switch (message.Command)
                {
                    case MessageCommandNames.AutoBlinkDuringFaceTracking:
                        _faceControlManager.PreferAutoBlinkOnWebCamTracking = message.ToBoolean();
                        break;
                    case MessageCommandNames.FaceDefaultFun:
                        _faceControlManager.DefaultBlendShape.FaceDefaultFunValue = message.ParseAsPercentage();
                        break;
                    case MessageCommandNames.EnableHeadRotationBasedBlinkAdjust:
                        _autoBlinkAdjust.EnableHeadRotationBasedBlinkAdjust = message.ToBoolean();
                        break;
                    case MessageCommandNames.EnableLipSyncBasedBlinkAdjust:
                        _autoBlinkAdjust.EnableLipSyncBasedBlinkAdjust = message.ToBoolean();
                        break;
                }
            });
        }
    }
}
