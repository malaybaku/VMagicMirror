using System;
using UnityEngine;
using UniVRM10;

namespace Baku.VMagicMirror.IK
{
    /// <summary>
    /// <see cref="BarracudaHand"/>で計算した結果をモーションに変換するやつ。
    /// 基本的にはBarracudaHandの下請けになる
    /// </summary>
    public class BarracudaHandIK : MonoBehaviour
    {
        private const float ReferenceArmLength = SettingAutoAdjuster.ReferenceArmLength;
        private const float StateEnterRequestCooldownAfterQuit = 0.6f;

        [SerializeField] private FingerController fingerController = null;

        //画像をタテに切っている関係で、画面中央に映った手 = だいたい肩の正面くらいに手がある、とみなしたい
        [SerializeField] private Vector3 rightHandOffset = new Vector3(0.25f, 0f, 0f);
        //wrist自体にはz座標が入っていないため、ちょっと手前に押し出しておく
        [SerializeField] private Vector3 commonAdditionalOffset = new Vector3(0f, 0f, 0.25f);
        //手と頭の距離にスケールをかけると、実際には頭の脇でちょこちょこ動かすだけのムーブを大きくできる
        [SerializeField] private Vector3 motionScale = Vector3.one;

        [SerializeField] private float positionSmoothFactor = 12f;
        [SerializeField] private float rotationSmoothFactor = 12f;
        //NOTE: 手の立ち上がりが早すぎてキモくなることがあるので制限する。
        //この制限が適用されるとき、角度のLerpも同様に低速化しなければならないことに要注意。
        [SerializeField] private float positionMaxSpeed = 1f;

        //頭が動いたとき、トラッキング中の手の移動量に強制的に加算するやつ。
        //本来なら0にしておいても良い値だが、見栄えのために入れる
        [Range(0f, 1f)]
        [SerializeField] private float headOffsetToHandFactor = 0.3f;

        [Header("Tracking Lost Motion")]
        [SerializeField] private float lostCount = 1f;
        [SerializeField] private float lostCircleMotionLerpFactor = 3f;
        [SerializeField] private float lostEndMotionLerpFactor = 12f;
        [SerializeField] private float lostMotionDuration = 1.0f;
        
        [Header("Misc")]
        [SerializeField] private ImageBaseHandLimitSetting handLimitSetting = null;

        private Vector3 LeftHandOffset => new Vector3(-rightHandOffset.x, rightHandOffset.y, rightHandOffset.z);
        
        public bool DisableHorizontalFlip { get; set; }
        public AlwaysDownHandIkGenerator DownHand { get; set; }

        private readonly BarracudaHandState _rightHandState = new BarracudaHandState(ReactedHand.Right);
        public IHandIkState RightHandState => _rightHandState;
        private readonly BarracudaHandState _leftHandState = new BarracudaHandState(ReactedHand.Left);
        public IHandIkState LeftHandState => _leftHandState;

        public bool ImageProcessActive { get; set; }


        private Vector3 _leftPosTarget;
        private Quaternion _leftRotTarget;
        private Vector3 _rightPosTarget;
        private Quaternion _rightRotTarget;

        private BarracudaHandFinger _finger;
        private HandIkGeneratorDependency _dependency;
        private ImageBaseHandRotLimiter _limiter;
        private BarracudaHandIkCalculator _ikCalculator;

        private Vector3[] _leftHandPoints;
        private Vector3[] _rightHandPoints;

        //モデルに対して定数みたくなるパラメータ
        private bool _hasModel;
        private Transform _head;
        private float _leftArmLengthFactor = 1f;
        private float _rightArmLengthFactor = 1f;
        private Vector3 _defaultHeadPosition = Vector3.up;
        
        private float _leftLostCount = 0f;
        private float _rightLostCount = 0f;

        private float _leftInitializeCooldown = 0f;
        private float _rightInitializeCooldown = 0f;
        
        public void Initialize(
            IVRMLoadable vrmLoadable,
            Vector3[] leftHandPoints,
            Vector3[] rightHandPoints
        )
        {
            _leftHandPoints = leftHandPoints;
            _rightHandPoints = rightHandPoints;
            
            _ikCalculator = new BarracudaHandIkCalculator(leftHandPoints, rightHandPoints);
            
            //TODO: 書く場所はココじゃないかもしれないが、頭との相対位置でIKを決めた方がキャリブとの相性が良いかもしれないので考えること
            vrmLoadable.VrmLoaded += info =>
            {
                _head = info.controlRig.GetBoneTransform(HumanBodyBones.Head);
                _defaultHeadPosition = _head.position;
                _ikCalculator.SetModel(info.controlRig, DownHand);
                InitializeArmLengthFactor(info.controlRig);
                _hasModel = true;
            };

            vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _ikCalculator.RemoveModel();
                _head = null;
            };

            _limiter = new ImageBaseHandRotLimiter(handLimitSetting);
        }
        
        
        public void SetupDependency(HandIkGeneratorDependency dependency)
        {
            _dependency = dependency;
        }
        
        /// <summary>
        /// <see cref="BarracudaHand"/>クラスがユーザーの左手をトラッキングできたとき呼び出すことで、
        /// 設定に応じて右手、または左手のIK情報を更新します。
        /// トラッキング結果は_leftHandPointsに載ってるため、このメソッドでは直接渡されません。
        /// </summary>
        public void UpdateLeftHand()
        {
            _leftLostCount = 0f;
            var rotInfo = _ikCalculator.CalculateLeftHandRotation();
            var headOffset = headOffsetToHandFactor * (_head.position - _defaultHeadPosition);

            if (DisableHorizontalFlip)
            {
                _leftPosTarget = 
                    _defaultHeadPosition + 
                    headOffset +
                    _leftArmLengthFactor * (
                        commonAdditionalOffset + MathUtil.Mul(motionScale, LeftHandOffset + _leftHandPoints[0])
                   );
                _leftRotTarget = _limiter.CalculateLeftHandRotation(
                    rotInfo.Rotation * Quaternion.AngleAxis(90f, Vector3.up)
                );
            }
            else
            {
                var p = _leftHandPoints[0];
                p.x = -p.x;
                _rightPosTarget = 
                    _defaultHeadPosition + 
                    headOffset +
                    _rightArmLengthFactor * (
                        commonAdditionalOffset + MathUtil.Mul(motionScale, rightHandOffset + p)
                    );
                var rightRot = rotInfo.Rotation;
                rightRot.y *= -1f;
                rightRot.z *= -1f;

                _rightRotTarget = _limiter.CalculateRightHandRotation(
                    rightRot * Quaternion.AngleAxis(-90f, Vector3.up)
                );
            }
            
            if (DisableHorizontalFlip && _leftInitializeCooldown <= 0f)
            {
                _leftHandState.RaiseRequestToUse();
            }
            else if (_rightInitializeCooldown <= 0f)
            {
                _rightHandState.RaiseRequestToUse();
            }

            //NOTE: 状態をチェックすることにより、「つねに手下げモード」時とかに指が動いてしまうのを防ぐ
            if ((DisableHorizontalFlip && _dependency.Config.LeftTarget.Value == HandTargetType.ImageBaseHand) ||
                (!DisableHorizontalFlip && _dependency.Config.RightTarget.Value == HandTargetType.ImageBaseHand)
            )
            {
                _finger.UpdateLeft(rotInfo.Forward, rotInfo.Up);
                _finger.ApplyLeftFingersDataToModel(DisableHorizontalFlip);
            }
        }

        /// <summary>
        /// <see cref="BarracudaHand"/>クラスがユーザーの右手をトラッキングできたとき呼び出すことで、
        /// 設定に応じて右手、または左手のIK情報を更新します。
        /// トラッキング結果は_leftHandPointsに載ってるため、このメソッドでは直接渡されません。
        /// </summary>
        public void UpdateRightHand()
        {
            _rightLostCount = 0f;
            var rotInfo = _ikCalculator.CalculateRightHandRotation();
            var headOffset = headOffsetToHandFactor * (_head.position - _defaultHeadPosition);

            if (DisableHorizontalFlip)
            {
                _rightPosTarget = 
                    _defaultHeadPosition +
                    headOffset +
                    _rightArmLengthFactor * (
                        commonAdditionalOffset + MathUtil.Mul(motionScale, rightHandOffset + _rightHandPoints[0])
                        );
                _rightRotTarget = _limiter.CalculateRightHandRotation(
                    rotInfo.Rotation * Quaternion.AngleAxis(-90f, Vector3.up)
                );
            }
            else
            {
                var p = _rightHandPoints[0];
                p.x = -p.x;
                _leftPosTarget = 
                    _defaultHeadPosition + 
                    headOffset + 
                    _leftArmLengthFactor * (
                        commonAdditionalOffset + MathUtil.Mul(motionScale, LeftHandOffset + p)
                        );
                var leftRot = rotInfo.Rotation;
                leftRot.y *= -1f;
                leftRot.z *= -1f;
                _leftRotTarget = _limiter.CalculateLeftHandRotation(
                    leftRot * Quaternion.AngleAxis(90f, Vector3.up)
                );
            }

            if (DisableHorizontalFlip && _rightInitializeCooldown <= 0f)
            {
                _rightHandState.RaiseRequestToUse();
            }
            else if (_leftInitializeCooldown <= 0f)
            {
                _leftHandState.RaiseRequestToUse();
            }
            
            //NOTE: 状態をチェックすることにより、「つねに手下げモード」時とかに指が動いてしまうのを防ぐ
            if ((DisableHorizontalFlip && _dependency.Config.RightTarget.Value == HandTargetType.ImageBaseHand) ||
                (!DisableHorizontalFlip && _dependency.Config.LeftTarget.Value == HandTargetType.ImageBaseHand)
            )
            {
                _finger.UpdateRight(rotInfo.Forward, rotInfo.Up);
                _finger.ApplyRightFingersDataToModel(DisableHorizontalFlip);
            }            
        }

        private void Start()
        {
            _leftPosTarget = _leftHandState.IKData.Position;
            _leftRotTarget = _leftHandState.IKData.Rotation;
            _rightPosTarget = _rightHandState.IKData.Position;
            _rightRotTarget = _rightHandState.IKData.Rotation;
            
            _finger = new BarracudaHandFinger(fingerController, _leftHandPoints, _rightHandPoints);
            _leftHandState.Finger = _finger;
            _rightHandState.Finger = _finger;

            _leftHandState.OnEnter += InitializeHandPosture;
            _rightHandState.OnEnter += InitializeHandPosture;

            _leftHandState.OnQuit += _ => _leftInitializeCooldown = StateEnterRequestCooldownAfterQuit;
            _rightHandState.OnQuit += _ => _rightInitializeCooldown = StateEnterRequestCooldownAfterQuit;
        }

        private void Update()
        {
            if (_leftInitializeCooldown > 0)
            {
                _leftInitializeCooldown -= Time.deltaTime;
            }

            if (_rightInitializeCooldown > 0)
            {
                _rightInitializeCooldown -= Time.deltaTime;
            }
        }
        

        public void UpdateIk()
        {
            if (!_hasModel)
            {
                return;
            }
            
            UpdateLostMotion();
            LerpIKPose();
        }

        private void LerpIKPose()
        {
            var leftPos = Vector3.Lerp(
                _leftHandState.IKData.Position, _leftPosTarget, positionSmoothFactor * Time.deltaTime
            );
            
            var leftDiff = leftPos - _leftHandState.IKData.Position;
            var leftSpeed = leftDiff.magnitude / Time.deltaTime;
            var leftSpeedRate = 1f;
            
            //速度が早すぎる場合は速度が律速になるよう低速化 + rotのLerpも弱める
            if (leftSpeed < positionMaxSpeed)
            {
                _leftHandState.IKData.Position = leftPos;
            }
            else
            {
                leftSpeedRate = positionMaxSpeed / leftSpeed;
                _leftHandState.IKData.Position += leftDiff * leftSpeedRate;
            }
            
            _leftHandState.IKData.Rotation = Quaternion.Slerp(
                _leftHandState.IKData.Rotation, 
                _leftRotTarget, 
                rotationSmoothFactor * Time.deltaTime * leftSpeedRate
            );

            var rightPos = Vector3.Lerp(
                _rightHandState.IKData.Position, _rightPosTarget, positionSmoothFactor * Time.deltaTime
                );
            var rightDiff = rightPos - _rightHandState.IKData.Position;
            var rightSpeed = rightDiff.magnitude / Time.deltaTime;
            var rightSpeedRate = 1f;
            
            if (rightSpeed < positionMaxSpeed)
            {
                _rightHandState.IKData.Position = rightPos;
            }
            else
            {
                rightSpeedRate = positionMaxSpeed / rightSpeed;
                _rightHandState.IKData.Position += rightDiff * rightSpeedRate;
            }
            
            _rightHandState.IKData.Rotation = Quaternion.Slerp(
                _rightHandState.IKData.Rotation, 
                _rightRotTarget, 
                rotationSmoothFactor * Time.deltaTime * rightSpeedRate
            );
            
        }

        private void UpdateLostMotion()
        {
            _leftLostCount += Time.deltaTime;
            _rightLostCount += Time.deltaTime;

            var circleMotionFactor = lostCircleMotionLerpFactor * Time.deltaTime;
            var endFactor = lostEndMotionLerpFactor * Time.deltaTime;

            if ((_leftLostCount > lostCount && DisableHorizontalFlip) ||
                (_rightLostCount > lostCount && !DisableHorizontalFlip))
            {
                var lostOver = DisableHorizontalFlip
                    ? _leftLostCount - lostCount
                    : _rightLostCount - lostCount;

                //NOTE: トラッキングロスモーションは2フェーズに分かれる。
                // 1. ロス直後: 「手を体の横に開いて下ろす」という円軌道寄りに手を持っていく
                // 2. それ以降: 単に手降ろし状態に持っていく
                if (lostOver < lostMotionDuration)
                {
                    var rate = lostOver / lostMotionDuration;
                    var factor = Mathf.Lerp(circleMotionFactor, endFactor, rate);
                    var pose = _ikCalculator.GetLostLeftHandPose(rate);
                    _leftPosTarget = Vector3.Lerp(_leftPosTarget, pose.position, factor);
                    _leftRotTarget = Quaternion.Slerp(_leftRotTarget, pose.rotation, factor);
                }
                else
                {
                    _leftPosTarget = Vector3.Lerp(_leftPosTarget, DownHand.LeftHand.Position, endFactor);
                    _leftRotTarget = Quaternion.Slerp(_leftRotTarget, DownHand.LeftHand.Rotation, endFactor);
                }
            }
            
            if ((_rightLostCount > lostCount && DisableHorizontalFlip) ||
                (_leftLostCount > lostCount && !DisableHorizontalFlip))
            {
                var lostOver = DisableHorizontalFlip
                    ? _rightLostCount - lostCount
                    : _leftLostCount - lostCount;

                //NOTE: トラッキングロスモーションは2フェーズに分かれる。
                // 1. ロス直後: 「手を体の横に開いて下ろす」という円軌道寄りに手を持っていく
                // 2. それ以降: 単に手降ろし状態に持っていく
                if (lostOver < lostMotionDuration)
                {
                    var rate = lostOver / lostMotionDuration;
                    var factor = Mathf.Lerp(circleMotionFactor, endFactor, rate);
                    var pose = _ikCalculator.GetLostRightHandPose(rate);
                    _rightPosTarget = Vector3.Lerp(_rightPosTarget, pose.position, factor);
                    _rightRotTarget = Quaternion.Slerp(_rightRotTarget, pose.rotation, factor);
                }
                else
                {
                    _rightPosTarget = Vector3.Lerp(_rightPosTarget, DownHand.RightHand.Position, endFactor);
                    _rightRotTarget = Quaternion.Slerp(_rightRotTarget, DownHand.RightHand.Rotation, endFactor);
                }
            }
        }        

        // 他のIKからこのIKに遷移した瞬間に呼び出すことで、直前のIKの姿勢をコピーして遷移をなめらかにする
        private void InitializeHandPosture(ReactedHand hand, IIKData src)
        {
            if (src == null)
            {
                return;
            }

            switch (hand)
            {
                case ReactedHand.Left:
                    _leftHandState.Position = src.Position;
                    _leftHandState.Rotation = src.Rotation;
                    break;
                case ReactedHand.Right:
                    _rightHandState.Position = src.Position;
                    _rightHandState.Rotation = src.Rotation;
                    break;
            }
        }

        private void InitializeArmLengthFactor(Vrm10RuntimeControlRig controlRig)
        {
            var leftUpperArmPos = controlRig.GetBoneTransform(HumanBodyBones.LeftUpperArm).position;
            var leftLowerArmPos = controlRig.GetBoneTransform(HumanBodyBones.LeftLowerArm).position;
            var leftHandPos = controlRig.GetBoneTransform(HumanBodyBones.LeftHand).position;
            var leftArmLength = 
                Vector3.Distance(leftUpperArmPos, leftLowerArmPos) +
                Vector3.Distance(leftLowerArmPos, leftHandPos);
            _leftArmLengthFactor = leftArmLength / ReferenceArmLength;
            
            var rightUpperArmPos = controlRig.GetBoneTransform(HumanBodyBones.RightUpperArm).position;
            var rightLowerArmPos = controlRig.GetBoneTransform(HumanBodyBones.RightLowerArm).position;
            var rightHandPos = controlRig.GetBoneTransform(HumanBodyBones.RightHand).position;
            var rightArmLength = 
                Vector3.Distance(rightUpperArmPos, rightLowerArmPos) +
                Vector3.Distance(rightLowerArmPos, rightHandPos);
            _rightArmLengthFactor = rightArmLength / ReferenceArmLength;
        }
        
        private class BarracudaHandState : IHandIkState
        {
            public BarracudaHandState(ReactedHand hand)
            {
                Hand = hand;
            }
            
            public bool SkipEnterIkBlend => false;
            public BarracudaHandFinger Finger { get; set; }
            
            public IKDataRecord IKData { get; } = new IKDataRecord();

            public Vector3 Position
            {
                get => IKData.Position;
                set => IKData.Position = value;
            } 
            
            public Quaternion Rotation
            {
                get => IKData.Rotation;
                set => IKData.Rotation = value;
            }

            public ReactedHand Hand { get; }
            public HandTargetType TargetType => HandTargetType.ImageBaseHand;

            public void RaiseRequestToUse() => RequestToUse?.Invoke(this);
            public event Action<IHandIkState> RequestToUse;

            public event Action<ReactedHand, IHandIkState> OnEnter;
            public event Action<ReactedHand> OnQuit;

            public void Enter(IHandIkState prevState) => OnEnter?.Invoke(Hand, prevState);

            public void Quit(IHandIkState nextState)
            {
                if (Hand == ReactedHand.Left)
                {
                    Finger?.ReleaseLeftHand();
                }
                else
                {
                    Finger?.ReleaseRightHand();
                }
                OnQuit?.Invoke(Hand);
            }
        }
    }
}
