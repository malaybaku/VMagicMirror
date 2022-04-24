using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// FaceSwitchやWtMが適用されているときに目を正面向きで固定したり、
    /// そうでない場合は回転量のスケールを効かせたりする処理。
    /// 他スクリプトのUpdateやLateUpdateで呼び出してボーン回転が適用済みのところに後処理として適用される。
    /// </summary>
    [DefaultExecutionOrder(20000)]
    public class EyeBonePostProcess : MonoBehaviour
    {
        public float Scale { get; set; } = 1.0f;
        /// <summary> 目を中央に固定したい場合、毎フレームtrueに設定する </summary>
        public bool ReserveReset { get; set; }
        /// <summary> 目の移動ウェイトを小さくしたい場合、毎フレーム指定する </summary>
        public float ReserveWeight { get; set; }

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
            //Face SwitchとかWord to Motionが指定されていれば、それを適用して終わり
            if (ReserveReset)
            {
                if (_hasEye)
                {
                    _leftEye.localRotation = Quaternion.identity;
                    _rightEye.localRotation = Quaternion.identity;
                }
                ReserveReset = false;
                return;
            }

            //それ以外の場合、回転量のスケーリングをしておく。
            //ただし、デフォルト設定時は何もしない。これはパフォーマンスと後方互換のカタさを両立するため
            if (_hasEye && (Scale < 0.995 || Scale > 1.005))
            {
                _leftEye.localRotation.ToAngleAxis(out var leftAngle, out var leftAxis);
                leftAngle = MathUtil.ClampAngle(leftAngle);
                //絞った範囲でスケーリングしてから入れ直す
                _leftEye.localRotation = Quaternion.AngleAxis(leftAngle * Scale, leftAxis);
                
                //leftEyeと同じ
                _rightEye.localRotation.ToAngleAxis(out var rightAngle, out var rightAxis);
                rightAngle = MathUtil.ClampAngle(rightAngle);
                _rightEye.localRotation = Quaternion.AngleAxis(rightAngle * Scale, rightAxis);
            }
        }
    }
}
