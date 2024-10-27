using Deform;
using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// キーボードの見える/見えないの制御。
    /// 普通のMagnetDeformerベースの処理に加えて、プレゼン/ペンタブモードでは右手のキーを非表示にする処理もサポートしている
    /// </summary>
    public class KeyboardVisibilityView : DeviceVisibilityBase
    {
        [SerializeField] private MeshRenderer rightHandMeshRenderer = null;
        [SerializeField] private MagnetDeformer rightHandMeshMagnetDeformer = null;

        protected override void OnRendererEnableUpdated(bool enable)
        {
            rightHandMeshRenderer.enabled = enable;
        }

        protected override void OnSetMagnetDeformerValue(float v)
        {
            rightHandMeshMagnetDeformer.Factor = v;
        }

        public void SetRightHandMeshRendererActive(bool active)
        {
            rightHandMeshRenderer.gameObject.SetActive(active);
        }
    }
}

