using UnityEngine;
using RootMotion.FinalIK;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// ひじをちょっと内側に折りたたむための処置。
    /// </summary>
    public class ElbowMotionModifier : MonoBehaviour
    {
        private const float WidthFactorLerp = 6.0f;
        
        [SerializeField] private HandIKIntegrator handIkIntegrator = null;
        [SerializeField] private BodyLeanIntegrator bodyLeanIntegrator = null;
        
        [Range(-5f, 5f)]
        [SerializeField] private float bodyRollRateToElbowWidthPlusFactor = 1.0f;

        [Range(-5f, 5f)]
        [SerializeField] private float bodyRollRateToElbowWidthMinusFactor = 1.0f;
        
        public float WaistWidthHalf { get; private set; } = 0.15f;
        public float ElbowCloseStrength { get; private set; } = 0.30f;
        
        public Vector3 RightElbowPositionOffset { get; set; }
        public Vector3 LeftElbowPositionOffset { get; set; }
        
        /// <summary>
        /// IKの効きを補正するファクターで0から1の値を指定します。
        /// </summary>
        public float ElbowIkRate { get; set; } = 1.0f;

        private float _leftWidthFactor = 1.0f;
        private float _rightWidthFactor = 1.0f;

        private bool _isInitialized = false;
        private Transform _leftArmBendGoal = null;
        private Transform _rightArmBendGoal = null;
        private FullBodyBipedIK _ik;


        public void SetWaistWidth(float width) => WaistWidthHalf = width * 0.5f;
        public void SetElbowCloseStrength(float strength) => ElbowCloseStrength = strength;

        [Inject]
        public void Initialize(IVRMLoadable vrmLoadable, IMessageReceiver receiver)
        {
            vrmLoadable.VrmLoaded += OnVrmLoaded;
            vrmLoadable.VrmDisposing += OnVrmDisposing;
            var _ = new ElbowMotionModifyReceiver(receiver, this);
        }

        private void Update()
        {
            if (!_isInitialized)
            {
                return;
            }

            float leftWidthFactorGoal =
                1.0f +
                (handIkIntegrator.IsLeftHandGripGamepad ? 1 : 0) +
                (bodyLeanIntegrator.BodyRollRate > 0
                    ? bodyLeanIntegrator.BodyRollRate * bodyRollRateToElbowWidthPlusFactor
                    : -bodyLeanIntegrator.BodyRollRate * bodyRollRateToElbowWidthMinusFactor
                );

            _leftWidthFactor = Mathf.Lerp(
                _leftWidthFactor,
                leftWidthFactorGoal,
                WidthFactorLerp * Time.deltaTime
            );

            //leftのときとプラスマイナスのファクターのかけかたが逆になることに注意
            float rightWidthFactorGoal = 
                1.0f + 
                (handIkIntegrator.IsRightHandGripGamepad ? 1 : 0) + 
                (bodyLeanIntegrator.BodyRollRate > 0
                    ? bodyLeanIntegrator.BodyRollRate * bodyRollRateToElbowWidthMinusFactor
                    : -bodyLeanIntegrator.BodyRollRate * bodyRollRateToElbowWidthPlusFactor
                );

            _rightWidthFactor = Mathf.Lerp(
                _rightWidthFactor,
                rightWidthFactorGoal,
                WidthFactorLerp * Time.deltaTime
            );

            _ik.solver.rightArmChain.bendConstraint.weight = ElbowCloseStrength * ElbowIkRate;
            _ik.solver.leftArmChain.bendConstraint.weight = ElbowCloseStrength * ElbowIkRate;

            _rightArmBendGoal.localPosition =
                new Vector3(WaistWidthHalf * _rightWidthFactor, 0, 0) + RightElbowPositionOffset;
            _leftArmBendGoal.localPosition =
                new Vector3(-WaistWidthHalf * _leftWidthFactor, 0, 0) + LeftElbowPositionOffset;
        }

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            _ik = info.vrmRoot.GetComponent<FullBodyBipedIK>();
            var spineBone = info.animator.GetBoneTransform(HumanBodyBones.Spine);

            _rightArmBendGoal = new GameObject().transform;
            _rightArmBendGoal.SetParent(spineBone);
            _rightArmBendGoal.localRotation = Quaternion.identity;
            _ik.solver.rightArmChain.bendConstraint.bendGoal = _rightArmBendGoal;

            _leftArmBendGoal = new GameObject().transform;
            _leftArmBendGoal.SetParent(spineBone);
            _leftArmBendGoal.localRotation = Quaternion.identity;
            _ik.solver.leftArmChain.bendConstraint.bendGoal = _leftArmBendGoal;
            
            _isInitialized = true;
        }

        private void OnVrmDisposing()
        {
            _ik = null;
            _rightArmBendGoal = null;
            _leftArmBendGoal = null;
            _isInitialized = false;
        }
    }
}