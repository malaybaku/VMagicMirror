using UnityEngine;
using Zenject;
using Baku.VMagicMirror.InterProcess;

namespace Baku.VMagicMirror
{
    [RequireComponent(typeof(FaceControlManager))]
    [RequireComponent(typeof(BehaviorBasedAutoBlinkAdjust))]
    public class FaceControlManagerReceiver : MonoBehaviour
    {
        private FaceControlManager _faceControlManager;
        private BehaviorBasedAutoBlinkAdjust _autoBlinkAdjust;

        //TODO: ManagerコードとかAdjusterクラスからうまくインスタンス作ったら非MonoBehaviour化できそうな。
        [Inject]
        public void Initialize(IMessageReceiver receiver)
        {
            receiver.AssignCommandHandler(
                MessageCommandNames.AutoBlinkDuringFaceTracking,
                message => 
                    _faceControlManager.PreferAutoBlinkOnWebCamTracking = message.ToBoolean()
                );

            receiver.AssignCommandHandler(
                MessageCommandNames.FaceDefaultFun,
                message =>
                    _faceControlManager.DefaultBlendShape.FaceDefaultFunValue = message.ParseAsPercentage()
                );
            receiver.AssignCommandHandler(
                MessageCommandNames.EnableHeadRotationBasedBlinkAdjust,
                message 
                    => _autoBlinkAdjust.EnableHeadRotationBasedBlinkAdjust = message.ToBoolean()
                );
            receiver.AssignCommandHandler(
                MessageCommandNames.EnableLipSyncBasedBlinkAdjust,
                message =>
                    _autoBlinkAdjust.EnableLipSyncBasedBlinkAdjust = message.ToBoolean()
                );
        }
        
        
        private void Start()
        {
            _faceControlManager = GetComponent<FaceControlManager>();
            _autoBlinkAdjust = GetComponent<BehaviorBasedAutoBlinkAdjust>();
        }
    }
}
