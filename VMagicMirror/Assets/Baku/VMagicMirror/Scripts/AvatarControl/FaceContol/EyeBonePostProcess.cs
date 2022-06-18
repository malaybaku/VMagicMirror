using UnityEngine;
using VRM;
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
        //NOTE: なぜ5倍かというと以下の理由による
        // - 従来作ってきたLookAtの可動域がだいたい7degくらい
        // - リアル人体の目がだいたいmaxで35degくらい可動する
        // - ので、VRMのCurveMapperは35degくらいまでのレンジに対応している事が期待できる
        private const float ConstAngleFactor = 5.0f;

        public float Scale { get; set; } = 1.0f;
        /// <summary> 目を中央に固定したい場合、毎フレームtrueに設定する </summary>
        public bool ReserveReset { get; set; }
        /// <summary> 目の移動ウェイトを小さくしたい場合、毎フレーム指定する </summary>
        public float ReserveWeight { get; set; }

        //private ExternalTrackerEyeJitter _externalTrackerEyeJitter;

        private bool _hasEye = false;
        private Transform _leftEye = null;
        private Transform _rightEye = null;

        private VRMLookAtHead _lookAtHead = null;

        [Inject]
        public void Initialize(IVRMLoadable vrmLoadable) //, ExternalTrackerEyeJitter externalTrackerEyeJitter)
        {
            
            //_externalTrackerEyeJitter = externalTrackerEyeJitter;

            vrmLoadable.VrmLoaded += info =>
            {
                _lookAtHead = info.vrmRoot.GetComponent<VRMLookAtHead>();
                _leftEye = info.animator.GetBoneTransform(HumanBodyBones.LeftEye);
                _rightEye = info.animator.GetBoneTransform(HumanBodyBones.RightEye);
                _hasEye = _lookAtHead != null && _leftEye != null && _rightEye != null;
            };
            vrmLoadable.VrmDisposing += () =>
            {
                _hasEye = false;
                _leftEye = null;
                _rightEye = null;
                _lookAtHead = null;
            };
        }
        
        private void Old_LateUpdate()
        {
            //Face SwitchとかWord to Motionが指定されている: scale == 0に相当するので計算を省いて終了
            //単にscale = 0として後半の計算に帰着しても良いが、まあ無駄が多いので…
            if (ReserveReset)
            {
                if (_hasEye)
                {
                    _leftEye.localRotation = Quaternion.identity;
                    _rightEye.localRotation = Quaternion.identity;
                }
                ReserveReset = false;
                ReserveWeight = 1f;
                return;
            }

            //NOTE: ここでやるくらいなら
            // if (_hasEye && !_externalTrackerEyeJitter.IsActive)
            // {
            //     var leftRot = _leftEye.localRotation;
            //     var rightRot = _rightEye.localRotation;
            //     _lookAtHead.LookWorldPosition();
            //
            //     _leftEye.localRotation *= leftRot;
            //     _rightEye.localRotation *= rightRot;
            // }
            
            var scale = Scale * ReserveWeight;
            ReserveWeight = 1f;

            if (_hasEye && (scale < 0.995 || scale > 1.005))
            {
                _leftEye.localRotation.ToAngleAxis(out var leftAngle, out var leftAxis);
                leftAngle = MathUtil.ClampAngle(leftAngle);
                //絞った範囲でスケーリングしてから入れ直す
                _leftEye.localRotation = Quaternion.AngleAxis(leftAngle * scale, leftAxis);
                
                //leftEyeと同じ
                _rightEye.localRotation.ToAngleAxis(out var rightAngle, out var rightAxis);
                rightAngle = MathUtil.ClampAngle(rightAngle);
                _rightEye.localRotation = Quaternion.AngleAxis(rightAngle * scale, rightAxis);
            }
            
            //NOTE: VRMの目ボーン角度マップを踏まえるとScaleはデカいほうが相性がよい
            // if (_hasEye && angleMapApplier.NeedOverwrite)
            // {
            //     _leftEye.localRotation = angleMapApplier.GetLeftEyeRotation(_leftEye.localRotation);
            //     _rightEye.localRotation = angleMapApplier.GetRightEyeRotation(_rightEye.localRotation);
            // }
        }
    }
}
