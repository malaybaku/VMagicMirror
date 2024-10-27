using Deform;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// キーボードの見える/見えないの制御。
    /// 普通のMagnetDeformerベースの処理に加えて、プレゼン/ペンタブモードでは右手のキーを非表示にする処理もやる
    /// </summary>
    public class KeyboardVisibility : DeviceVisibilityBase
    {
        [SerializeField] private MeshRenderer rightHandMeshRenderer = null;
        [SerializeField] private MagnetDeformer rightHandMeshMagnetDeformer = null;
        private KeyboardAndMouseMotionModes _motionModes = KeyboardAndMouseMotionModes.KeyboardAndTouchPad;

        protected override void OnRendererEnableUpdated(bool enable)
        {
            rightHandMeshRenderer.enabled = enable;
        }

        protected override void OnSetMagnetDeformerValue(float v)
        {
            rightHandMeshMagnetDeformer.Factor = v;
        }

        [Inject]
        public void SetupRightHandVisibility(IMessageReceiver receiver)
        {
            receiver.AssignCommandHandler(
                VmmCommands.SetKeyboardAndMouseMotionMode,
                message => SetMotionMode(message.ToInt()));
        }
        
        private void SetMotionMode(int modeIndex)
        {
            if (modeIndex >= 0 && modeIndex < (int) KeyboardAndMouseMotionModes.Unknown)
            {
                _motionModes = (KeyboardAndMouseMotionModes) modeIndex;
                rightHandMeshRenderer.gameObject.SetActive(_motionModes == KeyboardAndMouseMotionModes.KeyboardAndTouchPad);
            }
        }
    }
}

