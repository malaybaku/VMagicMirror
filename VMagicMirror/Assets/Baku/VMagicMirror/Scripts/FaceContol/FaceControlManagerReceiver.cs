using UnityEngine;
using UniRx;
using Zenject;

namespace Baku.VMagicMirror
{
    [RequireComponent(typeof(FaceControlManager))]
    [RequireComponent(typeof(BehaviorBasedAutoBlinkAdjust))]
    public class FaceControlManagerReceiver : MonoBehaviour
    {
        [Inject] private ReceivedMessageHandler handler = null;
        [Inject] private FaceControlConfiguration _config = null;

        private FaceControlManager _faceControlManager;
        private BehaviorBasedAutoBlinkAdjust _autoBlinkAdjust;

        private void Start()
        {
            _faceControlManager = GetComponent<FaceControlManager>();
            _autoBlinkAdjust = GetComponent<BehaviorBasedAutoBlinkAdjust>();
            handler.Commands.Subscribe(message =>
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
