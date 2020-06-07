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
                _rightEye = null;
                _leftEye = null;
                _hasValidEyeBone = false;
            };
        }
        
        private Transform _rightEye = null;
        private Transform _leftEye = null;
        private bool _hasValidEyeBone = false;

        private Quaternion _leftRotation = Quaternion.identity;
        private Quaternion _rightRotation = Quaternion.identity;
        
        public bool IsActive { get; set; }

        private void LateUpdate()
        {
            //NOTE: スムージングする場合はここでガードすると早すぎるので注意
            if (!_hasValidEyeBone || !IsActive)
            {
                return;
            }
            
            if (_tracker.Connected)
            {
                var leftX = 
                    _tracker.CurrentSource.Eye.LeftLookOut - _tracker.CurrentSource.Eye.LeftLookIn;
                var leftY =
                    _tracker.CurrentSource.Eye.LeftLookDown - _tracker.CurrentSource.Eye.LeftLookUp;
            
                var rightX = 
                    _tracker.CurrentSource.Eye.RightLookIn - _tracker.CurrentSource.Eye.RightLookOut;
                var rightY = 
                    _tracker.CurrentSource.Eye.RightLookDown - _tracker.CurrentSource.Eye.RightLookUp;
            
                _leftRotation = Quaternion.Euler(leftY * VerticalShapeToAngle, leftX * HorizontalShapeToAngle, 0);
                _rightRotation = Quaternion.Euler(rightY * VerticalShapeToAngle, rightX * HorizontalShapeToAngle, 0);
            }
            else
            {
                _leftRotation = Quaternion.identity;
                _rightRotation = Quaternion.identity;
            }
            
            //NOTE: 二重チェックは冗長なんだけど、「最初のガード文はのちのち外しそう」という予測も兼ねてガードしておきます
            if (_hasValidEyeBone && IsActive)
            {
                _rightEye.localRotation *= _leftRotation;
                _leftEye.localRotation *= _rightRotation;
            }
        }
    }
}
