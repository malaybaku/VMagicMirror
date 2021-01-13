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
        private readonly IKDataRecord _blendedRightHand = new IKDataRecord();
        public IIKGenerator RightHand => _blendedRightHand;
        
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

        private Vector3 _targetPosition = Vector3.zero;

        //入力(マウス移動 or ボタン操作)がない時間のカウント。
        private float _noInputCount = 0f;

        //この値で手下げ姿勢と手上げ姿勢をブレンドする。
        private float _handBlendRate = 1f;

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
            

            UpdateTimeout();
        }

        private void UpdateTimeout()
        {
            _noInputCount += Time.deltaTime;
            if (_noInputCount > HandIKIntegrator.AutoHandDownDuration)
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
                _blendedRightHand.Position = _rightHand.Position;
                _blendedRightHand.Rotation = _rightHand.Rotation;
            }
        }
    }
}

