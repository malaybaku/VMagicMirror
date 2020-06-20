using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// FaceSwitchやWtMが適用されているときに目を正面向きで固定するやつ。
    /// 他スクリプトのUpdateやLateUpdateで呼び出したのを後出しで適用する想定です
    /// </summary>
    [DefaultExecutionOrder(20000)]
    public class EyeBoneResetter : MonoBehaviour
    {
        public bool ReserveReset { get; set; }

        private bool _hasEye = false;
        private Transform _leftEye = null;
        private Transform _rightEye = null;

        [Inject]
        public void Initialize(IVRMLoadable vrmLoadable)
        {
            vrmLoadable.VrmLoaded += info =>
            {
                _leftEye = info.animator.GetBoneTransform(HumanBodyBones.LeftEye);
                _rightEye = info.animator.GetBoneTransform(HumanBodyBones.RightEye);
                _hasEye = (_leftEye != null && _rightEye != null);
            };
            vrmLoadable.VrmDisposing += () =>
            {
                _hasEye = false;
                _leftEye = null;
                _rightEye = null;
            };
        }
        
        private void LateUpdate()
        {
            if (_hasEye && ReserveReset)
            {
                _leftEye.localRotation = Quaternion.identity;
                _rightEye.localRotation = Quaternion.identity;
            }
            ReserveReset = false;
        }
    }
}
