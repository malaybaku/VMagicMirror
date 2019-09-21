using UnityEngine;
using UniRx;

namespace Baku.VMagicMirror
{
    [RequireComponent(typeof(FaceControlManager))]
    public class FaceControlManagerReceiver : MonoBehaviour
    {
        [SerializeField] private ReceivedMessageHandler handler = null;

        private FaceControlManager _faceControlManager;

        private void Start()
        {
            _faceControlManager = GetComponent<FaceControlManager>();
            handler.Commands.Subscribe(message =>
            {
                switch (message.Command)
                {
                    case MessageCommandNames.AutoBlinkDuringFaceTracking:
                        _faceControlManager.PreferAutoBlink = message.ToBoolean();
                        break;
                    case MessageCommandNames.FaceDefaultFun:
                        _faceControlManager.DefaultBlendShape.FaceDefaultFunValue = message.ParseAsPercentage();
                        break;
                }
            });
        }
    }
}
