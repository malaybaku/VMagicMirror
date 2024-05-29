using System;
using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror.IK
{
    /// <summary>
    /// このクラスができる計算
    /// - 角度に対して両手のIK位置を決める
    /// - とくに、途中の持ち替え操作に対応している
    /// </summary>
    public class CarHandleIkGenerator : HandIkGeneratorBase
    {
        private const float HandleGripChangeDuration = 0.3f;
        
        private const float AngleUpperOffset = 30f;
        private const float AngleDownDiff = 50f;
        private const float AngleUpDiff = 120f;

        private readonly CarHandleAngleGenerator _angleGenerator;
        private readonly CarHandleProvider _provider;
        private readonly CarHandleFingerController _fingerController;
        
        private Transform CenterOfHandle => _provider.OffsetAddedTransform;
        private float HandleRadius => _provider.CarHandleRadius;
        private float CurrentAngle => _angleGenerator.HandleAngle;

        public CarHandleIkGenerator(
            HandIkGeneratorDependency dependency,
            CarHandleAngleGenerator angleGenerator,
            CarHandleProvider provider,
            CarHandleFingerController fingerController
            ) : base(dependency)
        {
            _angleGenerator = angleGenerator;
            _provider = provider;
            _fingerController = fingerController;

            _leftHandState = new HandleHandState(
                this, ReactedHand.Left, 150f, 150f, 60f,
                Quaternion.Euler(90f, 90f, 0)
                );
            _rightHandState = new HandleHandState(
                this, ReactedHand.Right, 30f, 60f, 150f,
                Quaternion.Euler(-90f, -90f, 0)
                );
            
            //該当モードでスティックに触ると両手がハンドル用IKになる: 片手ずつでもいいかもだが
            dependency.Events.MoveLeftGamepadStick += v =>
            {
                if (dependency.Config.IsAlwaysHandDown.Value || 
                    dependency.Config.GamepadMotionMode.Value != GamepadMotionModes.CarController)
                {
                    return;
                }

                _leftHandState.RaiseRequest();
                _rightHandState.RaiseRequest();
            };

            dependency.Events.MoveRightGamepadStick += v =>
            {
                if (dependency.Config.IsAlwaysHandDown.Value || 
                    dependency.Config.GamepadMotionMode.Value != GamepadMotionModes.CarController)
                {
                    return;
                }

                _leftHandState.RaiseRequest();
                _rightHandState.RaiseRequest();
            };
        }
        
        private readonly HandleHandState _leftHandState;
        public override IHandIkState LeftHandState => _leftHandState;
        private readonly HandleHandState _rightHandState;
        public override IHandIkState RightHandState => _rightHandState;
        public float WristToTipLength { get; set; } = 0.12f;
        
        public override void Start()
        {
            _leftHandState.HandleTransform = CenterOfHandle;
            _leftHandState.GripChangeMoveDuration = HandleGripChangeDuration;

            _rightHandState.HandleTransform = CenterOfHandle;
            _rightHandState.GripChangeMoveDuration = HandleGripChangeDuration;

            _leftHandState.DefaultAngle = 180f - AngleUpperOffset;
            _leftHandState.AngleMinusDiff = AngleUpDiff;
            _leftHandState.AnglePlusDiff = AngleDownDiff;

            _rightHandState.DefaultAngle = AngleUpperOffset;
            _rightHandState.AngleMinusDiff = AngleDownDiff;
            _rightHandState.AnglePlusDiff = AngleUpDiff;
        }
        
        public override void Update()
        {
            if (!(Dependency.Config.LeftTarget.Value is HandTargetType.CarHandle ||
                Dependency.Config.RightTarget.Value is HandTargetType.CarHandle))
            {
                return;
            }

            _leftHandState.HandleRadius = HandleRadius;
            _leftHandState.WristToPalmLength = WristToTipLength * 0.5f;
            _rightHandState.HandleRadius = HandleRadius;
            _rightHandState.WristToPalmLength = WristToTipLength * 0.5f;
            
            //NOTE: スティックの右方向が正にする場合、 `angle = -stick.x` みたいな関係になりうるので注意 
            var dt = Time.deltaTime;
            _leftHandState.HandleAngle = CurrentAngle;
            _rightHandState.HandleAngle = CurrentAngle;
            _leftHandState.Update(dt);
            _rightHandState.Update(dt);

            _provider.SetSteeringRotation(_angleGenerator.HandleAngle);
            UpdateFingerState();
        }

        private void UpdateFingerState()
        {
            if (Dependency.Config.LeftTarget.Value is HandTargetType.CarHandle)
            {
                if (_leftHandState.IsGripping.Value)
                {
                    _fingerController.GripLeftHand();
                }
                else
                {
                    _fingerController.ReleaseLeftHand();       
                }
            }

            if (Dependency.Config.RightTarget.Value is HandTargetType.CarHandle)
            {
                if (_rightHandState.IsGripping.Value)
                {
                    _fingerController.GripRightHand();
                }
                else
                {
                    _fingerController.ReleaseRightHand();       
                }
            }
        }
        
        class HandleHandState : IHandIkState
        {
            // NOTE: 角度は真右を始点、反時計周りを正としてdegreeで指定する(例外は都度書く)

            public HandleHandState(
                CarHandleIkGenerator parent, ReactedHand hand, 
                float defaultAngle, float angleMinusDiff, float anglePlusDiff,
                Quaternion rotationOffset)
            {
                _parent = parent;
                Hand = hand;
                DefaultAngle = defaultAngle;
                AngleMinusDiff = angleMinusDiff;
                AnglePlusDiff = anglePlusDiff;
                _rotationOffset = rotationOffset;
            }

            private readonly Quaternion _rotationOffset;
            private readonly CarHandleIkGenerator _parent;
            
            /// <summary> ハンドルが0度のときに掴んでる角度 </summary>
            public float DefaultAngle { get; set; }
            
            /// <summary> 掴み角度デフォルトの角度からこの値だけマイナスすると、それ以上は握っていられない…という値を正の値で指定する </summary>
            public float AngleMinusDiff { get; set; }
            
            /// <summary> デフォルトの角度からこの値だけプラスすると、それ以上は握っていられない…という値を正の値で指定する </summary>
            public float AnglePlusDiff { get; set; }

            /// <summary>
            /// ゲームパッドのスティック等で定まる、ハンドルが回転しているべき角度。正負や、 > +360 の値などに意味があるので注意
            /// ※このクラス自体はハンドルの角度を純粋なinputにする(手が追いつかないから回せない、とかはない)ことに注意！
            /// </summary>
            public float HandleAngle { get; set; }

            //入力系の値で、アバターや空間編集するときだけ変化するやつ
            public float HandleRadius { get; set; } = 0.2f;
            public Transform HandleTransform { get; set; }

            public float NonGripZOffset { get; set; } = -.1f;
            
            //出力系で、公開する値
            private readonly ReactiveProperty<Pose> _currentPose = new (Pose.identity);
            public IReadOnlyReactiveProperty<Pose> CurrentPose => _currentPose;

            //NOTE: 指の制御のために使ってもいいような値
            private readonly ReactiveProperty<bool> _isGripping = new(false);
            public IReadOnlyReactiveProperty<bool> IsGripping => _isGripping;

            //NOTE: 正であることが必須
            public float GripChangeMoveDuration { get; set; } = 0.3f;

            //NOTE: ハンドルの持ち替え回数を示す値で、1回握り直すたびに増える
            private int _gripChangeCount;
            
            private float _prevHandleAngle;
            private float _gripChangeMotionCount = 0;
            private Pose _gripMotionStartPose = Pose.identity;
            
            public void Update(float deltaTime)
            {
                var (gripChangeCount, targetAngle) = CalculateHandleTarget();
                if (gripChangeCount != _gripChangeCount)
                {
                    //NOTE: _isGrippingがfalseのときにここを通過し直すとモーションのdurationを数え直して握りモーションをやり直す。
                    //ここを何度も通ると永遠にハンドルを握れなくなるが、見た目がよほどアレじゃなければ許容する
                    _gripChangeMotionCount = 0f;
                    _isGripping.Value = false;
                    _gripChangeCount = gripChangeCount;
                    _gripMotionStartPose = _currentPose.Value;
                }

                if (_isGripping.Value)
                {
                    _currentPose.Value = GetHandleGrippedPose(targetAngle);
                }
                else
                {
                    _currentPose.Value = GetHandleNonGrippedPose(
                        _gripMotionStartPose,
                        GetHandleGrippedPose(targetAngle),
                        _gripChangeMotionCount / GripChangeMoveDuration
                    );

                    _gripChangeMotionCount += deltaTime;
                    if (_gripChangeMotionCount >= GripChangeMoveDuration)
                    {
                        _isGripping.Value = true;
                    }
                }
            }

            private (int gripChangeCount, float angle) CalculateHandleTarget()
            {
                // _innerTargetAngleの決まり方
                // - 片手だけで (Default - MinDiff) ~ (Default + MaxDiff) の幅でハンドルを回してHandleAngleまで回転する、
                //   という冪等な操作を想定して計算する。
                var handleAngle = HandleAngle;

                //ほぼ正位置のハンドル
                if (handleAngle > -AngleMinusDiff && handleAngle < AnglePlusDiff)
                {
                    return (0, HandleAngle + DefaultAngle);
                }

                var angleWidth = AnglePlusDiff + AngleMinusDiff;
                
                //左にぐるぐる回ってるハンドル
                if (handleAngle > AnglePlusDiff)
                {
                    var gripChangeCount = Mathf.FloorToInt((HandleAngle - AnglePlusDiff) / angleWidth) + 1;
                    var angle = DefaultAngle - AngleMinusDiff + Mathf.Repeat(HandleAngle - AnglePlusDiff, angleWidth);
                    return (gripChangeCount, angle);
                }

                //右にぐるぐる回ってるハンドル
                else
                {
                    var gripChangeCount = Mathf.FloorToInt((-HandleAngle - AngleMinusDiff) / angleWidth) - 1;
                    var angle = DefaultAngle + AnglePlusDiff - Mathf.Repeat(-HandleAngle - AngleMinusDiff, angleWidth);
                    return (gripChangeCount, angle);
                }
            }
            
            private Pose GetHandleGrippedPose(float angle)
            {
                var t = HandleTransform;
                var localForward = t.forward;
                
                var rotation = t.rotation * Quaternion.AngleAxis(angle, Vector3.forward) * _rotationOffset;
                var position =
                    t.position +
                    localForward * (-WristToPalmLength) +
                    Quaternion.AngleAxis(angle, localForward) * (HandleRadius * t.right);
    
                return new Pose(position, rotation);
            }

            private Pose GetHandleNonGrippedPose(Pose startPose, Pose endPose, float rate)
            {
                //ポイント:
                // - 持ち替えるときにハンドルのちょっと手前側にIKが来る
                // - rateは適当にsmoothしておく

                rate = Mathf.SmoothStep(0f, 1f, rate);
                
                var zOffsetRate = 1f;
                if (rate < 0.3f)
                {
                    zOffsetRate = rate / 0.3f;
                }
                else if (rate > 0.7f)
                {
                    zOffsetRate = (1 - rate) / 0.3f;
                }
                var positionOffset = (zOffsetRate * NonGripZOffset) * HandleTransform.forward;

                //TODO: Quaternionコレでキレイにならないのでは？
                return new Pose(
                    Vector3.Lerp(startPose.position, endPose.position, rate) + positionOffset,
                    Quaternion.Slerp(startPose.rotation, endPose.rotation, rate)
                );
            }
            
            #region IHandIkState
            
            public bool SkipEnterIkBlend => false;
            
            public Vector3 Position => _currentPose.Value.position;
            public Quaternion Rotation => _currentPose.Value.rotation;
            
            public ReactedHand Hand { get; }
            public HandTargetType TargetType => HandTargetType.CarHandle;
            public float WristToPalmLength { get; set; } = 0.06f;

            public event Action<IHandIkState> RequestToUse;

            public void RaiseRequest() => RequestToUse?.Invoke(this);

            //NOTE: 横着でGrip表現にGamepadFingerを使っているが、たぶんバランスが悪いはずなので別で用意して欲しい
            public void Enter(IHandIkState prevState)
            {
                if (Hand == ReactedHand.Left)
                {
                    _parent.Dependency.Reactions.GamepadFinger.GripLeftHand();
                }
                else
                {
                    _parent.Dependency.Reactions.GamepadFinger.GripRightHand();
                }
            }

            public void Quit(IHandIkState nextState)
            {
                if (Hand == ReactedHand.Left)
                {
                    _parent.Dependency.Reactions.GamepadFinger.ReleaseLeftHand();
                }
                else
                {
                    _parent.Dependency.Reactions.GamepadFinger.ReleaseRightHand();
                }
            }
            
            #endregion
        }
    }
}
