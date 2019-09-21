using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>マウス位置を元に、右手のあるべき姿勢を求めるやつ</summary>
    public class MouseMoveHandIKGenerator : MonoBehaviour
    {
        private readonly IKDataRecord _rightHand = new IKDataRecord();
        public IIKGenerator RightHand => _rightHand;

        //手をあまり厳格にキーボードに沿わせると曲がり過ぎるのでゼロ回転側に寄せるファクター
        private const float WristYawApplyFactor = 0.5f;
        private const float WristYawSpeedFactor = 12f;
        
        //手首ではなく手のひらあたりにマウスがあるように見えるための補正値
        public float HandToPalmLength { get; set; } = 0.05f;

        public float YOffset { get; set; } = 0.03f;

        [SerializeField]
        public TouchPadProvider _touchPad = null;

        [SerializeField]
        private float _speedFactor = 12f;
        
        private Vector3 YOffsetAlwaysVec => YOffset * Vector3.up;

        private Vector3 _targetPosition = Vector3.zero;

        private void Update()
        {
            _rightHand.Position = Vector3.Lerp(
                _rightHand.Position,
                _targetPosition,
                _speedFactor * Time.deltaTime
                );

            //体の中心、手首、中指の先が1直線になるような向きへ水平に手首を曲げたい
            var rot = Quaternion.Slerp(
                Quaternion.Euler(Vector3.up * (
                    -Mathf.Atan2(_rightHand.Position.z, _rightHand.Position.x) *
                    Mathf.Rad2Deg)),
                Quaternion.Euler(Vector3.up * (-90)),
                WristYawApplyFactor
                );

            _rightHand.Rotation = Quaternion.Slerp(
                _rightHand.Rotation, rot, WristYawSpeedFactor * Time.deltaTime
                );
        }

        public void MoveMouse(Vector3 mousePosition)
        {
            int x = (int)mousePosition.x;
            int y = (int)mousePosition.y;

            float xClamped = Mathf.Clamp(x - Screen.width * 0.5f, -1000, 1000) / 1000.0f;
            float yClamped = Mathf.Clamp(y - Screen.height * 0.5f, -1000, 1000) / 1000.0f;
            var targetPos = _touchPad.GetHandTipPosFromScreenPoint(xClamped, yClamped) + YOffsetAlwaysVec;
            targetPos -= HandToPalmLength * new Vector3(targetPos.x, 0, targetPos.z).normalized;

            _targetPosition = targetPos;
        }

    }
}

