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

        //腰の真横より後ろにbendGoalを持っていくことで肘が前方に行きにくくするための補正値。
        [SerializeField] private float bendGoalZOffset = -0.02f;

        [Range(0f, 1f)] 
        [SerializeField] private float ikWeightMinOnImageHandTracking = 0.8f;
        
        public float WaistWidthHalf { get; private set; } = 0.15f;
        public float BendGoalIkWeight { get; private set; } = 0.30f;
        
        public Vector3 RightElbowPositionOffset { get; set; }
        public Vector3 LeftElbowPositionOffset { get; set; }
        
        /// <summary>
        /// IKの効きを補正するファクターで0から1の値を指定します。
        /// </summary>
        public float ElbowIkRate { get; set; } = 1.0f;

        private HandIKIntegrator _handIKIntegrator;        

        private float _leftWidthFactor = 1.0f;
        private float _rightWidthFactor = 1.0f;

        private bool _hasModel = false;
        private Transform _leftArmBendGoal = null;
        private Transform _rightArmBendGoal = null;
        private FullBodyBipedIK _ik;


        public void SetWaistWidth(float width) => WaistWidthHalf = width * 0.5f;
        public void SetBendGoalIkWeight(float weight) => BendGoalIkWeight = weight;

        [Inject]
        public void Initialize(
            IVRMLoadable vrmLoadable,
            IMessageReceiver receiver,
            HandIKIntegrator handIKIntegrator)
        {
            vrmLoadable.VrmLoaded += OnVrmLoaded;
            vrmLoadable.VrmDisposing += OnVrmDisposing;
            receiver.AssignCommandHandler(
                VmmCommands.SetWaistWidth,
                message => SetWaistWidth(message.ParseAsCentimeter())
            );
            
            // NOTE: Command名を直してもいい (Unity側の内部で使ってる値のほうが実体には近い
            receiver.AssignCommandHandler(
                VmmCommands.SetElbowCloseStrength,
                message => SetBendGoalIkWeight(message.ParseAsPercentage())
            );

            _handIKIntegrator = handIKIntegrator;
        }

        private void Update()
        {
            if (!_hasModel)
            {
                return;
            }

            var leftWidthFactorGoal =
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
            var rightWidthFactorGoal = 
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

            _ik.solver.leftArmChain.bendConstraint.weight = GetIkWeight(handIkIntegrator.LeftTargetType.Value);
            _ik.solver.rightArmChain.bendConstraint.weight = GetIkWeight(handIkIntegrator.RightTargetType.Value);

            _leftArmBendGoal.localPosition =
                new Vector3(-WaistWidthHalf * _leftWidthFactor, 0, bendGoalZOffset) + LeftElbowPositionOffset;
            _rightArmBendGoal.localPosition =
                new Vector3(WaistWidthHalf * _rightWidthFactor, 0, bendGoalZOffset) + RightElbowPositionOffset;
        }

        private float GetIkWeight(HandTargetType type)
        {
            // NOTE: 画像ベースでハンドトラッキングしている場合、BendGoalのウェイトが小さいとひじが暴れて見栄えがかなり悪化するので、
            // 強制でweightを大きくする。ただし、もともとIKのweightが高い設定の場合はもとの設定が優先される
            if (type is HandTargetType.ImageBaseHand)
            {
                return Mathf.Max(BendGoalIkWeight, ikWeightMinOnImageHandTracking) * ElbowIkRate;
            }
            else
            {
                return BendGoalIkWeight * ElbowIkRate;
            }
        }
        
        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            _ik = info.fbbIk;
            var spineBone = info.controlRig.GetBoneTransform(HumanBodyBones.Spine);

            _rightArmBendGoal = new GameObject().transform;
            _rightArmBendGoal.SetParent(spineBone);
            _rightArmBendGoal.localRotation = Quaternion.identity;
            _ik.solver.rightArmChain.bendConstraint.bendGoal = _rightArmBendGoal;

            _leftArmBendGoal = new GameObject().transform;
            _leftArmBendGoal.SetParent(spineBone);
            _leftArmBendGoal.localRotation = Quaternion.identity;
            _ik.solver.leftArmChain.bendConstraint.bendGoal = _leftArmBendGoal;
            
            _hasModel = true;
        }

        private void OnVrmDisposing()
        {
            _ik = null;
            _rightArmBendGoal = null;
            _leftArmBendGoal = null;
            _hasModel = false;
        }
    }
}