using System;
using R3;
using UnityEngine;

namespace Baku.VMagicMirror.IK
{
    /// <summary>マウス位置を元に、右手のあるべき姿勢を求めるやつ</summary>
    /// <remarks>
    /// マウス位置は右手のみを管理してるのを踏まえて、わかりやすさのためクラス自体でIHandIkStateを実装してます
    /// </remarks>
    public class MouseMoveHandIKGenerator : HandIkGeneratorBase, IHandIkState
    {
        //手をあまり厳格にキーボードに沿わせると曲がり過ぎるのでゼロ回転側に寄せるファクター
        private const float WristYawApplyFactor = 0f; //0.5f;
        private const float WristYawSpeedFactor = 12f;
        private const float SpeedFactor = 12f;
        
        private readonly IKDataRecord _rightHand = new();
        private readonly IKDataRecord _blendedRightHand = new();

        private readonly Subject<string> _mouseClickMotionStarted = new();
        public Observable<string> MouseClickMotionStarted => _mouseClickMotionStarted;
        
        #region IHandIkState

        public bool SkipEnterIkBlend => false;
        public Vector3 Position => _blendedRightHand.Position;
        public Quaternion Rotation => _blendedRightHand.Rotation;
        public ReactedHand Hand => ReactedHand.Right;
        public HandTargetType TargetType => HandTargetType.Mouse;
        public event Action<IHandIkState> RequestToUse;
        public void Enter(IHandIkState prevState)
        {
            ResetHandDownTimeout(true);
        }

        public void Quit(IHandIkState nextState)
        {
            if (nextState.TargetType != HandTargetType.Keyboard)
            {
                Dependency.Reactions.FingerController.ReleaseRightHandTyping();
            }
        }
        
        #endregion
        
        public float HandToTipLength { get; set; } = 0.12f;

        public float YOffset { get; set; } = 0.03f;

        public bool EnableUpdate { get; set; } = true;
        
        /// <summary> 直近で参照したタッチパッドのワールド座標。 </summary>
        public Vector3 ReferenceTouchpadPosition { get; private set; }

        /// <summary> 一定時間入力がないとき手降ろし姿勢に遷移すべきかどうか </summary>
        public bool EnableHandDownTimeout { get; set; } = true;
        
        private readonly TouchPadProvider _touchPad;

        private Vector3 YOffsetAlwaysVec => YOffset * Vector3.up;

        //NOTE: HandIkIntegratorから初期化で入れてもらう
        public AlwaysDownHandIkGenerator DownHand { get; set; }

        
        public override IHandIkState LeftHandState => null;
        public override IHandIkState RightHandState => this;

        /// <summary> マウス入力が一定時間以上ないと立つフラグ </summary>
        public bool IsNoInputTimeOutReached => _noInputCount > HandIKIntegrator.AutoHandDownDuration;

        private Vector3 _targetPosition = Vector3.zero;

        //入力(マウス移動 or ボタン操作)がない時間のカウント。
        private float _noInputCount = 0f;

        //この値で手下げ姿勢と手上げ姿勢をブレンドする。
        private float _handBlendRate = 1f;

        public MouseMoveHandIKGenerator(HandIkGeneratorDependency dependency, TouchPadProvider touchPadProvider)
            : base(dependency)
        {
            _touchPad = touchPadProvider;
            
            //読み方はそのままで、タッチパッドを使いたいときマウス移動イベントが届いたらマウスに切り替えたくなる
            dependency.Events.MoveMouse += _ =>
            {
                if (dependency.Config.KeyboardAndMouseMotionMode.Value ==
                      KeyboardAndMouseMotionModes.KeyboardAndTouchPad)
                {
                    RequestToUse?.Invoke(this);

                    if (dependency.Config.RightTarget.Value == HandTargetType.Mouse)
                    {
                        dependency.Reactions.ParticleStore.RequestMouseMoveParticle(ReferenceTouchpadPosition);
                        ResetHandDownTimeout(false);
                    }
                }
            };

            dependency.Events.OnMouseButton += eventName =>
            {
                if (dependency.Config.KeyboardAndMouseMotionMode.Value ==
                    KeyboardAndMouseMotionModes.KeyboardAndTouchPad)
                {
                    RequestToUse?.Invoke(this);
                }

                //マウスはButtonUpでもエフェクトを出す。
                //ちょっとうるさくなるが、意味的にはMouseのButtonUpはけっこうデカいアクションなので
                if (dependency.Config.RightTarget.Value == HandTargetType.Mouse)
                {
                    dependency.Reactions.FingerController.OnMouseButton(eventName);
                    dependency.Reactions.ParticleStore.RequestMouseClickParticle();
                    ResetHandDownTimeout(false);
                    _mouseClickMotionStarted.OnNext(eventName);
                }
            };
        }

        public override void Start()
        {
            //NOTE: この値は初期値が大外れしていないことを保証するものなので、多少ズレていてもOK
            _rightHand.Position = _touchPad.GetHandTipPosFromScreenPoint() + YOffsetAlwaysVec;
            _targetPosition = _rightHand.Position;
        }
            
        public override void Update()
        {
            if (!EnableUpdate)
            {
                return;
            }

            ReferenceTouchpadPosition = _touchPad.GetHandTipPosFromScreenPoint();
            _targetPosition = ReferenceTouchpadPosition + _touchPad.GetOffsetVector(YOffset, HandToTipLength);
            
            _rightHand.Position = Vector3.Lerp(
                _rightHand.Position,
                _targetPosition,
                SpeedFactor * Time.deltaTime
                );

            //体の中心、手首、中指の先が1直線になるような向きへ水平に手首を曲げたい
            var rot = Quaternion.Slerp(
                _touchPad.GetWristRotation(_rightHand.Position),
                Quaternion.Euler(Vector3.up * (-90)),
                WristYawApplyFactor
                );

            _rightHand.Rotation = Quaternion.Slerp(
                _rightHand.Rotation, rot, WristYawSpeedFactor * Time.deltaTime
                );
            

            UpdateTimeout();
        }

        private void UpdateTimeout()
        {
            if (EnableHandDownTimeout)
            {
                _noInputCount += Time.deltaTime;
            }
            else
            {
                //NOTE: タイムアウトの値をリセットし続ける: この方がブレンディングが飛ばないため性質がよい
                _noInputCount = 0f;
            }
            
            if (Dependency.Config.CheckKeyboardAndMouseHandsCanMoveDown())
            {
                _handBlendRate = Mathf.Max(0f, _handBlendRate - HandIKIntegrator.HandDownBlendSpeed * Time.deltaTime);
            }
            else
            {
                _handBlendRate = Mathf.Min(1f, _handBlendRate + HandIKIntegrator.HandUpBlendSpeed * Time.deltaTime);
            }

            //NOTE: 多くの場合ブレンド計算は不要。
            if (_handBlendRate > 0.999f)
            {
                _blendedRightHand.Position = _rightHand.Position;
                _blendedRightHand.Rotation = _rightHand.Rotation;
            }
            else if (_handBlendRate < 0.001f)
            {
                _blendedRightHand.Position = DownHand.RightHand.Position;
                _blendedRightHand.Rotation = DownHand.RightHand.Rotation;
            }
            else
            {
                var rate = Mathf.SmoothStep(0, 1, _handBlendRate);
                _blendedRightHand.Position = Vector3.Lerp(
                    DownHand.RightHand.Position, _rightHand.Position, rate
                );
                _blendedRightHand.Rotation = Quaternion.Slerp(
                    DownHand.RightHand.Rotation, _rightHand.Rotation, rate
                );
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="refreshIkImmediate"></param>
        public void ResetHandDownTimeout(bool refreshIkImmediate)
        {
            _noInputCount = 0;
            if (refreshIkImmediate)
            {
                _handBlendRate = 1f;
                _blendedRightHand.Position = _rightHand.Position;
                _blendedRightHand.Rotation = _rightHand.Rotation;
            }
        }
    }
}

