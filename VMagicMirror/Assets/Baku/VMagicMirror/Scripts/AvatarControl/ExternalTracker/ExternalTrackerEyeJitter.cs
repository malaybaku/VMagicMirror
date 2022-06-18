using UnityEngine;
using Zenject;
using Baku.VMagicMirror.ExternalTracker;

namespace Baku.VMagicMirror
{
    /// <summary> 外部トラッキングによる眼球運動をやるやつです </summary>
    public class ExternalTrackerEyeJitter : MonoBehaviour, IEyeRotationRequestSource
    {
        //NOTE: Jitterと言ってるが値としてはユーザーの眼球運動そのものなので、大きめの運動として取り扱う
        private const float HorizontalShapeToRate = 1f;
        private const float VerticalShapeToRate = 1f;
        private const float TotalBoneRotationRateLimit = 1f;
        
        private ExternalTrackerDataSource _tracker;

        [Inject]
        public void Initialize(ExternalTrackerDataSource externalTracker)
        {
            _tracker = externalTracker;
        }

        public bool IsTracked => (_tracker != null) && _tracker.Connected;
        
        public bool IsActive { get; set; }
        public Vector2 LeftEyeRotationRate { get; private set; }
        public Vector2 RightEyeRotationRate { get; private set; }

        private void LateUpdate()
        {
            //NOTE: ここでガードしないようにして状態によらずスムージングする、みたいな実装もありうる
            if (!IsActive)
            {
                return;
            }
            
            var leftX = 
                _tracker.CurrentSource.Eye.LeftLookIn - _tracker.CurrentSource.Eye.LeftLookOut;
            var leftY =
                _tracker.CurrentSource.Eye.LeftLookUp - _tracker.CurrentSource.Eye.LeftLookDown;
        
            var rightX = 
                _tracker.CurrentSource.Eye.RightLookOut - _tracker.CurrentSource.Eye.RightLookIn;
            var rightY = 
                _tracker.CurrentSource.Eye.RightLookUp - _tracker.CurrentSource.Eye.RightLookDown;

            if (_tracker.DisableHorizontalFlip)
            {
                leftX = -leftX;
                rightX = -rightX;
            }

            //NOTE: ClampMagnitudeがあるとかえって不自然かもしれない(iOS側で面倒見てくれてる説もある)。外してもよいかも
            LeftEyeRotationRate = Vector2.ClampMagnitude(
                new Vector2(leftX * HorizontalShapeToRate, leftY * VerticalShapeToRate),
                TotalBoneRotationRateLimit
                );
            RightEyeRotationRate = Vector2.ClampMagnitude(
                new Vector2(rightX * HorizontalShapeToRate, rightY * VerticalShapeToRate),
                TotalBoneRotationRateLimit
                );

            // //NOTE: 二重チェックは冗長なんだけど、「最初のガード文はのちのち外しそう」という予測も兼ねてガード
            // if (!(_hasValidEyeBone && IsActive))
            // {
            //     return;
            // }
            //
            // //ボーンの曲げすぎをガードしつつ後付回転で適用
            //
            // var resultLeftRotation = _leftRotation * _leftEye.localRotation;
            // resultLeftRotation.ToAngleAxis(out var leftAngle, out var leftAxis);
            // leftAngle = Mathf.Repeat(leftAngle + 180f, 360f) - 180f;
            // if (Mathf.Abs(leftAngle) > TotalBoneRotationLimit)
            // {
            //     leftAngle = Mathf.Sign(leftAngle) * TotalBoneRotationLimit;
            //     resultLeftRotation = Quaternion.AngleAxis(leftAngle, leftAxis);
            // }
            // _leftEye.localRotation = resultLeftRotation;
            //
            //
            // var resultRightRotation = _rightRotation * _rightEye.localRotation;
            // resultRightRotation.ToAngleAxis(out var rightAngle, out var rightAxis);
            // rightAngle = Mathf.Repeat(rightAngle + 180f, 360f) - 180f;
            // if (Mathf.Abs(rightAngle) > TotalBoneRotationLimit)
            // {
            //     rightAngle = Mathf.Sign(rightAngle) * TotalBoneRotationLimit;
            //     resultRightRotation = Quaternion.AngleAxis(rightAngle, rightAxis);
            // }
            // _rightEye.localRotation = resultRightRotation;            
        }
    }
}
