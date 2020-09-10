using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>マウス位置を元に、右手のあるべき姿勢を求めるやつ</summary>
    public class MouseMoveHandIKGenerator : HandIkGeneratorBase
    {
        //手をあまり厳格にキーボードに沿わせると曲がり過ぎるのでゼロ回転側に寄せるファクター
        private const float WristYawApplyFactor = 0f; //0.5f;
        private const float WristYawSpeedFactor = 12f;
        private const float SpeedFactor = 12f;
        
        private readonly IKDataRecord _rightHand = new IKDataRecord();
        public IIKGenerator RightHand => _rightHand;
        
        public float HandToTipLength { get; set; } = 0.12f;

        public float YOffset { get; set; } = 0.03f;

        public bool EnableUpdate { get; set; } = true;
        
        /// <summary> 直近で参照したタッチパッドのワールド座標。 </summary>
        public Vector3 ReferenceTouchpadPosition { get; private set; }
        
        private readonly TouchPadProvider _touchPad;

        private Vector3 YOffsetAlwaysVec => YOffset * Vector3.up;

        private Vector3 _targetPosition = Vector3.zero;

        
        public MouseMoveHandIKGenerator(MonoBehaviour coroutineResponder, TouchPadProvider touchPadProvider)
            : base(coroutineResponder)
        {
            _touchPad = touchPadProvider;
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
        }
    }
}

