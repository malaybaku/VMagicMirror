using UnityEngine;
using Zenject;
using Baku.VMagicMirror.ExternalTracker;

namespace Baku.VMagicMirror
{
    /// <summary> 外部トラッキングによる眼球運動をやるやつです </summary>
    public class ExternalTrackerEyeJitter : MonoBehaviour
    {
        //NOTE: けっこう大きくしてもいいんですねえコレ…知らんかった
        private const float HorizontalShapeToAngle = 10.0f;
        private const float VerticalShapeToAngle = 10.0f;
        private ExternalTrackerDataSource _tracker;
        private const float TotalBoneRotationLimit = 10.0f;

        [Inject]
        public void Initialize(IVRMLoadable vrmLoadable, ExternalTrackerDataSource externalTracker)
        {
            _tracker = externalTracker;
            
            vrmLoadable.VrmLoaded += info =>
            {
                _rightEye = info.animator.GetBoneTransform(HumanBodyBones.RightEye);
                _leftEye = info.animator.GetBoneTransform(HumanBodyBones.LeftEye);
                _hasValidEyeBone = (_rightEye != null && _leftEye != null);
            };
            
            vrmLoadable.VrmDisposing += () =>
            {
                _hasValidEyeBone = false;
                _rightEye = null;
                _leftEye = null;
            };
        }
        
        private Transform _rightEye = null;
        private Transform _leftEye = null;
        private bool _hasValidEyeBone = false;

        private Quaternion _leftRotation = Quaternion.identity;
        private Quaternion _rightRotation = Quaternion.identity;

        public bool IsTracked => (_tracker != null) && _tracker.Connected;
        
        public bool IsActive { get; set; }

        private void LateUpdate()
        {
            //NOTE: スムージングする場合はここでガードすると早すぎるので注意
            if (!_hasValidEyeBone || !IsActive)
            {
                return;
            }
            
            var leftX = 
                _tracker.CurrentSource.Eye.LeftLookIn - _tracker.CurrentSource.Eye.LeftLookOut;
            var leftY =
                _tracker.CurrentSource.Eye.LeftLookDown - _tracker.CurrentSource.Eye.LeftLookUp;
        
            var rightX = 
                _tracker.CurrentSource.Eye.RightLookOut - _tracker.CurrentSource.Eye.RightLookIn;
            var rightY = 
                _tracker.CurrentSource.Eye.RightLookDown - _tracker.CurrentSource.Eye.RightLookUp;
        
            _leftRotation = Quaternion.Euler(leftY * VerticalShapeToAngle, leftX * HorizontalShapeToAngle, 0);
            _rightRotation = Quaternion.Euler(rightY * VerticalShapeToAngle, rightX * HorizontalShapeToAngle, 0);
            
            //NOTE: 二重チェックは冗長なんだけど、「最初のガード文はのちのち外しそう」という予測も兼ねてガード
            if (!(_hasValidEyeBone && IsActive))
            {
                return;
            }

            //ボーンの曲げすぎをガードしつつ後付回転で適用
            
            var resultLeftRotation = _leftRotation * _leftEye.localRotation;
            resultLeftRotation.ToAngleAxis(out var leftAngle, out var leftAxis);
            leftAngle = Mathf.Repeat(leftAngle + 180f, 360f) - 180f;
            if (Mathf.Abs(leftAngle) > TotalBoneRotationLimit)
            {
                leftAngle = Mathf.Sign(leftAngle) * TotalBoneRotationLimit;
                resultLeftRotation = Quaternion.AngleAxis(leftAngle, leftAxis);
            }
            _leftEye.localRotation = resultLeftRotation;

            
            var resultRightRotation = _rightRotation * _rightEye.localRotation;
            resultRightRotation.ToAngleAxis(out var rightAngle, out var rightAxis);
            rightAngle = Mathf.Repeat(rightAngle + 180f, 360f) - 180f;
            if (Mathf.Abs(rightAngle) > TotalBoneRotationLimit)
            {
                rightAngle = Mathf.Sign(rightAngle) * TotalBoneRotationLimit;
                resultRightRotation = Quaternion.AngleAxis(rightAngle, rightAxis);
            }
            _rightEye.localRotation = resultRightRotation;
        }
    }
}
