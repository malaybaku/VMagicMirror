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
    
    public class CarHandleIkGeneratorSingle : MonoBehaviour
    {
        [SerializeField] private Transform leftHandIkTarget = null;
        [SerializeField] private Transform rightHandIkTarget = null;
        [SerializeField] private Transform bodyRotationTarget = null;

        [SerializeField] private Transform centerOfHandle = null;
        [SerializeField] private float handleRadius = 0.4f;
        [SerializeField] private float bodyRotationAngleLimit = 20f;
        [Range(0.1f, 0.5f)]
        [SerializeField] private float handleGripChangeDuration = 0.3f;

        [SerializeField] private Transform handleRotationVisual = null;

        [SerializeField] private float angleUpperOffset = 30f;
        [SerializeField] private float angleDownDiff = 60f;
        [SerializeField] private float angleUpDiff = 150f;
        
        [SerializeField] private AnimationCurve angleToHeadYawRateCurve;
        
        [Range(-540, 540)] [SerializeField] private float currentAngle = 0;

        private readonly HandleHandState _leftHandState = new(150f, 150f, 60f);
        private readonly HandleHandState _rightHandState = new(30f, 60f, 150f);

        private readonly ReactiveProperty<float> _bodyRotationRate = new(0f);
        //NOTE: 1になると最大限まで左に傾く
        public IReadOnlyReactiveProperty<float> BodyRotationRate => _bodyRotationRate;

        private readonly ReactiveProperty<float> _eyeRotationRate = new(0f);
        public IReadOnlyReactiveProperty<float> EyeRotationRate => _eyeRotationRate;

        private readonly ReactiveProperty<float> _headYawRotationRate = new(0);
        public IReadOnlyReactiveProperty<float> HeadYawRotationRate => _headYawRotationRate;

        private static float Sigmoid(float value, float factor, float pow)
        {
            return 2f / (1 + Mathf.Pow(pow, -value / factor)) - 1f;
        }

        private float GetBodyRotationRate(float angle) => Sigmoid(angle, 180f, 4);

        private float GetHeadRotationRate(float angle)
        {
            //NOTE: 0~90degあたりにほぼ不感になるエリアが欲しいのでカーブを使ってます
            var rate = Mathf.Clamp01(Mathf.Abs(angle / 540));
            return Mathf.Sign(angle) * angleToHeadYawRateCurve.Evaluate(rate);
        }

        private float GetEyeRotationRate(float angle) => Sigmoid(angle, 85f, 4);
        
        private void Update()
        {
            if (leftHandIkTarget == null ||
                rightHandIkTarget == null ||
                centerOfHandle == null ||
                handleRotationVisual == null)
            {
                return;
            }

            _leftHandState.HandleTransform = centerOfHandle;
            _leftHandState.HandleRadius = handleRadius;
            _leftHandState.GripChangeMoveDuration = handleGripChangeDuration;

            _rightHandState.HandleTransform = centerOfHandle;
            _rightHandState.HandleRadius = handleRadius;
            _rightHandState.GripChangeMoveDuration = handleGripChangeDuration;

            _leftHandState.DefaultAngle = 180f - angleUpperOffset;
            _leftHandState.AngleMinusDiff = angleUpDiff;
            _leftHandState.AnglePlusDiff = angleDownDiff;

            _rightHandState.DefaultAngle = angleUpperOffset;
            _rightHandState.AngleMinusDiff = angleDownDiff;
            _rightHandState.AnglePlusDiff = angleUpDiff;
           
            
            //NOTE: スティックの右方向が正にする場合、 `angle = -stick.x` みたいな関係になりうるので注意 

            var dt = Time.deltaTime;
            _leftHandState.HandleAngle = currentAngle;
            _rightHandState.HandleAngle = currentAngle;
            _leftHandState.Update(dt);
            _rightHandState.Update(dt);
            _bodyRotationRate.Value = GetBodyRotationRate(currentAngle);
            _headYawRotationRate.Value = GetHeadRotationRate(currentAngle);
            _eyeRotationRate.Value = GetEyeRotationRate(currentAngle);

            ApplyCurrentPoses();
        }

        //NOTE: ここは本来別のクラスでやってほしい
        private void ApplyCurrentPoses()
        {
            handleRotationVisual.localRotation = Quaternion.AngleAxis(currentAngle, Vector3.forward);

            var leftPose = _leftHandState.CurrentPose.Value;
            var rightPose = _rightHandState.CurrentPose.Value;
            leftHandIkTarget.SetPositionAndRotation(_leftHandState.CurrentPose.Value.position, leftPose.rotation);
            rightHandIkTarget.SetPositionAndRotation(rightPose.position, rightPose.rotation);

            if (bodyRotationTarget != null)
            {
                bodyRotationTarget.localRotation = 
                    Quaternion.AngleAxis(bodyRotationAngleLimit * BodyRotationRate.Value, Vector3.forward);
            }
        }

        class HandleHandState
        {
            // NOTE: 角度は真右を始点、反時計周りを正としてdegreeで指定する(例外は都度書く)

            public HandleHandState(float defaultAngle, float angleMinusDiff, float anglePlusDiff)
            {
                DefaultAngle = defaultAngle;
                AngleMinusDiff = angleMinusDiff;
                AnglePlusDiff = anglePlusDiff;
            }
            
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
                if (HandleTransform == null)
                {
                    return;
                }

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
                
                var rotation = t.rotation * Quaternion.AngleAxis(angle, Vector3.forward);
                var position =
                    t.position +
                    Quaternion.AngleAxis(angle, t.forward) * (HandleRadius * t.right);
    
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
        }
    }
}
