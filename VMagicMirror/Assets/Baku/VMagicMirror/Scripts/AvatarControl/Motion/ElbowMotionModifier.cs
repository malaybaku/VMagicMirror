using Baku.VMagicMirror.MediaPipeTracker;
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
        
        [SerializeField] private float cutOffFrequency = .5f;
        [SerializeField] private float imageBaseWeightUpSpeed = 5f;
        [SerializeField] private float imageBaseWeightDownSpeed = 5f;
        [SerializeField] private float imageBaseWeightMax = 2f;

        public float WaistWidthHalf { get; private set; } = 0.15f;
        public float BendGoalIkWeight { get; private set; } = 0.30f;
        
        public Vector3 RightElbowPositionOffset { get; set; }
        public Vector3 LeftElbowPositionOffset { get; set; }
        
        /// <summary>
        /// IKの効きを補正するファクターで0から1の値を指定します。
        /// </summary>
        public float ElbowIkRate { get; set; } = 1.0f;

        private HandIKIntegrator _handIKIntegrator;
        private MediaPipeKinematicSetter _mediaPipeKinematic;

        private float _leftWidthFactor = 1.0f;
        private float _rightWidthFactor = 1.0f;
        
        // webカメラのトラッキングベースの肘の位置の適用率
        private float _leftImageBasePositionWeight = 0f;
        private float _rightImageBasePositionWeight = 0f;
        // NOTE: 下記の情報も基本的にはHipsからのlocalPosition基準になっている(連続性をキープしたいので)
        private readonly BiQuadFilterVector3 _leftImageBasedPosition = new();
        private readonly BiQuadFilterVector3 _rightImageBasedPosition = new();
        private Vector3? _latestLeftImageBasedRawPosition;
        private Vector3? _latestRightImageBasedRawPosition;

        private bool _hasModel = false;
        private Transform _leftArmBendGoal = null;
        private Transform _rightArmBendGoal = null;
        private Transform _hips;
        private Transform _vrmRoot;
        private FullBodyBipedIK _ik;

        // webカメラで肩-肘の位置関係がわかってるときにbendGoalの位置を特定するために使う値
        private Vector3 _leftUpperArm;
        private Vector3 _rightUpperArm;
        private float _leftUpperArmLength;
        private float _rightUpperArmLength;


        public void SetWaistWidth(float width) => WaistWidthHalf = width * 0.5f;
        public void SetBendGoalIkWeight(float weight) => BendGoalIkWeight = weight;

        [Inject]
        public void Initialize(
            IVRMLoadable vrmLoadable,
            IMessageReceiver receiver,
            HandIKIntegrator handIKIntegrator,
            MediaPipeKinematicSetter mediaPipeKinematic)
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
            _mediaPipeKinematic = mediaPipeKinematic;

            _leftImageBasedPosition.SetUpAsLowPassFilter(60f, cutOffFrequency * Vector3.one);
            _rightImageBasedPosition.SetUpAsLowPassFilter(60f, cutOffFrequency * Vector3.one);
        }

        private void Update()
        {
            if (!_hasModel)
            {
                return;
            }

            UpdateWidthFactor();
            UpdateImageBasedElbowPosition(Time.deltaTime);

            _ik.solver.leftArmChain.bendConstraint.weight = GetIkWeight(handIkIntegrator.LeftTargetType.CurrentValue);
            _ik.solver.rightArmChain.bendConstraint.weight = GetIkWeight(handIkIntegrator.RightTargetType.CurrentValue);

            UpdateBendGoalPosition();
        }

        private void UpdateWidthFactor()
        {
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
        }

        private void UpdateImageBasedElbowPosition(float deltaTime)
        {
            var rootPos = _vrmRoot.position;
            var rootRotation = Quaternion.Euler(0, _vrmRoot.rotation.eulerAngles.y, 0);
            
            var leftHandTargetType = handIkIntegrator.LeftTargetType.CurrentValue;
            if (leftHandTargetType is HandTargetType.ImageBaseHand && _mediaPipeKinematic.LeftShoulderToElbow.HasValue)
            {
                var shoulderToElbowInImage = _mediaPipeKinematic.LeftShoulderToElbow.Value;
                var shoulderToElbow = ElbowOrientationCalculator.CalculateLeftElbowDirection(shoulderToElbowInImage);

                var leftWorldPosition = rootPos + rootRotation * (
                    _leftUpperArm +
                    new Vector3(0, 0, bendGoalZOffset) +
                    shoulderToElbow * _leftUpperArmLength
                    );
                var leftLocalPosition = _hips.InverseTransformPoint(leftWorldPosition);

                if (_latestLeftImageBasedRawPosition.HasValue)
                {
                    // 前の値もある状態で値を検出してる -> スムージングしながら更新
                    _leftImageBasedPosition.Update(leftLocalPosition);
                }
                else
                {
                    // トラッキング開始時点での値
                    _leftImageBasedPosition.ResetValue(leftLocalPosition);
                }
                _latestLeftImageBasedRawPosition = leftLocalPosition;
                _leftImageBasePositionWeight = 
                    Mathf.Clamp(_leftImageBasePositionWeight + imageBaseWeightUpSpeed * deltaTime, 0f, imageBaseWeightMax);
            }
            else
            {
                if (_latestLeftImageBasedRawPosition != null)
                {
                    _leftImageBasedPosition.Update(_latestLeftImageBasedRawPosition.Value);
                }
                _leftImageBasePositionWeight =
                    Mathf.Clamp(_leftImageBasePositionWeight - imageBaseWeightDownSpeed * deltaTime, 0f, imageBaseWeightMax);
            }

            var rightHandTargetType = handIkIntegrator.RightTargetType.CurrentValue;
            if (rightHandTargetType is HandTargetType.ImageBaseHand && _mediaPipeKinematic.RightShoulderToElbow.HasValue)
            {
                var shoulderToElbowInImage = _mediaPipeKinematic.RightShoulderToElbow.Value;
                var shoulderToElbow = ElbowOrientationCalculator.CalculateRightElbowDirection(shoulderToElbowInImage);
                
                var rightWorldPosition = rootPos + rootRotation * (
                    _rightUpperArm +
                    new Vector3(0, 0, bendGoalZOffset) +
                    shoulderToElbow * _rightUpperArmLength
                    );
                var rightLocalPosition = _hips.InverseTransformPoint(rightWorldPosition);
                
                if (_latestRightImageBasedRawPosition.HasValue)
                {
                    _rightImageBasedPosition.Update(rightLocalPosition);
                }
                else
                {
                    // トラッキング開始時点での値
                    _rightImageBasedPosition.ResetValue(rightLocalPosition);
                }
                _latestRightImageBasedRawPosition = rightLocalPosition;
                _rightImageBasePositionWeight = 
                    Mathf.Clamp(_rightImageBasePositionWeight + imageBaseWeightUpSpeed * deltaTime, 0f, imageBaseWeightMax);
            }
            else
            {
                if (_latestRightImageBasedRawPosition != null)
                {
                    _rightImageBasedPosition.Update(_latestRightImageBasedRawPosition.Value);
                }
                _rightImageBasePositionWeight =
                    Mathf.Clamp(_rightImageBasePositionWeight - imageBaseWeightDownSpeed * deltaTime, 0f, imageBaseWeightMax);
            }
        }

        private void UpdateBendGoalPosition()
        {
            var defaultLeftBendGoal =
                new Vector3(-WaistWidthHalf * _leftWidthFactor, 0, bendGoalZOffset) + LeftElbowPositionOffset;
            if (_leftImageBasePositionWeight > 0)
            {
                _leftArmBendGoal.localPosition = Vector3.Lerp(
                    defaultLeftBendGoal,
                    _leftImageBasedPosition.Output,
                    _leftImageBasePositionWeight
                );
            }
            else
            {
                _leftArmBendGoal.localPosition = defaultLeftBendGoal;
                // しばらく肘トラッキングの情報が来なければ捨てる
                _latestLeftImageBasedRawPosition = null;
            }
            
            var defaultRightBendGoal =
                new Vector3(WaistWidthHalf * _rightWidthFactor, 0, bendGoalZOffset) + RightElbowPositionOffset;
            if (_rightImageBasePositionWeight > 0)
            {
                _rightArmBendGoal.localPosition = Vector3.Lerp(
                    defaultRightBendGoal,
                    _rightImageBasedPosition.Output,
                    _rightImageBasePositionWeight
                );
            }
            else
            {
                _rightArmBendGoal.localPosition = defaultRightBendGoal;
                _latestRightImageBasedRawPosition = null;
            }
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

            _leftUpperArm = info.animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position;
            var leftLowerArm = info.animator.GetBoneTransform(HumanBodyBones.LeftLowerArm).position;
            _leftUpperArmLength = Vector3.Distance(_leftUpperArm, leftLowerArm);

            _rightUpperArm = info.animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position;
            var rightLowerArm = info.animator.GetBoneTransform(HumanBodyBones.RightLowerArm).position;
            _rightUpperArmLength = Vector3.Distance(_rightUpperArm, rightLowerArm);
            
            _vrmRoot = info.vrmRoot;
            _hips = info.animator.GetBoneTransform(HumanBodyBones.Hips);
            _hasModel = true;
        }

        private void OnVrmDisposing()
        {
            _hasModel = false;

            _ik = null;
            _rightArmBendGoal = null;
            _leftArmBendGoal = null;
            _vrmRoot = null;
            _hips = null;
        }
    }
}