using System;
using R3;
using UnityEngine;

namespace Baku.VMagicMirror.IK
{
    /// <summary>マウス位置に基づいてペンタブ用の、右手っぽい動きをするやつ</summary>
    public class PenTabletHandIKGenerator : HandIkGeneratorBase, IHandIkState
    {
        private const float WristYawSpeedFactor = 12f;
        private const float SpeedFactor = 12f;
        private const float PressOffset = 0.01f;
        
        private readonly IKDataRecord _rightHand = new IKDataRecord();

        #region IHandIkState

        public bool SkipEnterIkBlend => false;
        public Vector3 Position => _rightHand.Position;
        public Quaternion Rotation => _rightHand.Rotation;
        public ReactedHand Hand => ReactedHand.Right;
        public HandTargetType TargetType => HandTargetType.PenTablet;
        public event Action<IHandIkState> RequestToUse;
        public void Enter(IHandIkState prevState)
        {
            _penTablet.SetHandOnPenTablet(true);
            Dependency.Reactions.FingerController.RightHandPenTabletMode = true;
            // _fingerController.GripRightHand();
        }

        public void Quit(IHandIkState nextState)
        {
            _penTablet.SetHandOnPenTablet(false);
            Dependency.Reactions.FingerController.RightHandPenTabletMode = false;
            // _fingerController.ReleaseRightHand();
        }
        
        #endregion

        //消したい！が、消せなさそう
        public float YOffset { get; set; } = 0.03f;

        public bool EnableUpdate { get; set; } = true;

        private Vector3 _wristToPenBasePosition = Vector3.zero;
        private float _wristDefaultFloat = 0f;
        
        private readonly PenTabletProvider _penTablet;
        private readonly PenTabletFingerController _fingerController;
        
        public override IHandIkState LeftHandState => null;
        public override IHandIkState RightHandState => this;

        private readonly Subject<string> _mouseClickMotionStarted = new();
        public Observable<string> MouseClickMotionStarted => _mouseClickMotionStarted;

        private Vector3 _targetPosition = Vector3.zero;

        //左クリック中かどうか。trueの場合、押し込み動作が発生
        private bool _isLeftButtonDown = false;
        //右、中クリック中かどうか。どちらかがtrueの場合、回転として反映
        private bool _isRightButtonDown = false;
        private bool _isMidButtonDown = false;
        
        public PenTabletHandIKGenerator(
            HandIkGeneratorDependency dependency, IVRMLoadable vrmLoadable, PenTabletProvider penTabletProvider
            ) : base(dependency)
        {
            _penTablet = penTabletProvider;
            _fingerController = new PenTabletFingerController(dependency.Reactions.FingerController);

            vrmLoadable.VrmLoaded += info =>
            {
                var wrist = info.controlRig.GetBoneTransform(HumanBodyBones.RightHand);
                var wristPosition = wrist.position;
                var midProximal = info.controlRig.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
                var midInter = info.controlRig.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate);
                var littleDistal = info.controlRig.GetBoneTransform(HumanBodyBones.RightLittleDistal);
                var thumbProximal = info.controlRig.GetBoneTransform(HumanBodyBones.RightThumbIntermediate);
                
                //NOTE: 人差し指の第2-第3関節の横でペンが縦に立つように調節するとこういう式になります
                _wristToPenBasePosition = (midProximal != null && midInter != null && thumbProximal != null)
                    ? (midProximal.position * 0.5f + midInter.position * 0.5f - wristPosition) +
                      new Vector3(0, 0, (thumbProximal.position - wristPosition).z)
                    : Vector3.zero;

                //小指がめり込まないように…みたいな措置。
                //やや余裕を取りすぎてる節はあるが、まあ良いでしょう
                _wristDefaultFloat = littleDistal != null 
                    ? (wristPosition.z - littleDistal.position.z) + PressOffset
                    : 0.05f + PressOffset;
            };
            
            //読み方はそのままで、ペンタブを使いたいときマウス移動イベントが届いたらペンタブのIKを適用したがる
            dependency.Events.MoveMouse += _ =>
            {
                if (dependency.Config.KeyboardAndMouseMotionMode.Value == KeyboardAndMouseMotionModes.PenTablet)
                {
                    RequestToUse?.Invoke(this);
                }

                if (dependency.Config.RightTarget.Value == HandTargetType.PenTablet)
                {
                    dependency.Reactions.ParticleStore.RequestPenTabletMoveParticle(
                        _penTablet.GetPenTipPosition(),
                        _penTablet.GetRawRotation()
                        );
                }
            };

            dependency.Events.OnMouseButton += eventName =>
            {
                if (dependency.Config.KeyboardAndMouseMotionMode.Value == KeyboardAndMouseMotionModes.PenTablet)
                {
                    RequestToUse?.Invoke(this);
                }

                switch (eventName)
                {
                    case MouseButtonEventNames.LDown:
                        _isLeftButtonDown = true;
                        break;
                    case MouseButtonEventNames.LUp:
                        _isLeftButtonDown = false;
                        break;
                    case MouseButtonEventNames.RDown:
                        _isRightButtonDown = true;
                        break;
                    case MouseButtonEventNames.RUp:
                        _isRightButtonDown = false;
                        break;
                    case MouseButtonEventNames.MDown:
                        _isMidButtonDown = true;
                        break;
                    case MouseButtonEventNames.MUp:
                        _isMidButtonDown = false;
                        break;
                }

                //ButtonUpでもエフェクトを出す。波紋系のエフェクトだとしっくり来るはず
                if (dependency.Config.RightTarget.Value == HandTargetType.PenTablet)
                {
                    dependency.Reactions.ParticleStore.RequestPenTabletClickParticle();
                    _mouseClickMotionStarted.OnNext(eventName);
                }
            };
        }

        public override void Start()
        {
            //NOTE: この値は初期値が大外れしていないことを保証するものなので、多少ズレていてもOK
            _targetPosition = _rightHand.Position = _penTablet.GetPosFromScreenPoint();
        }
            
        public override void Update()
        {
            if (!EnableUpdate)
            {
                return;
            }
            
            var baseRot = _penTablet.GetBaseRotation();
            
            _targetPosition =
                _penTablet.GetPosFromScreenPoint() -
                (baseRot * Quaternion.AngleAxis(-90f, Vector3.up)) * _wristToPenBasePosition +
                _penTablet.Normal * (_wristDefaultFloat + YOffset);

            _rightHand.Position = Vector3.Lerp(
                _rightHand.Position,
                _targetPosition,
                SpeedFactor * Time.deltaTime
                );

            //NOTE:手を完全に立てるのも変なので70度くらいにしておく。移動中に傾くような動作があっても良い…のかもしれない。
            var wristRoll = (_isRightButtonDown || _isMidButtonDown) ? -35f : -40f;
            var rot =
                baseRot *
                Quaternion.AngleAxis(_isLeftButtonDown ? 0f : -10f, Vector3.right) *
                Quaternion.AngleAxis(wristRoll, Vector3.forward) *
                Quaternion.AngleAxis(-90f, Vector3.up);
            
            _rightHand.Rotation = Quaternion.Slerp(
                _rightHand.Rotation, rot, WristYawSpeedFactor * Time.deltaTime
                );
        }
    }
}

