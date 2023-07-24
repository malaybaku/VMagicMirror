using Baku.VMagicMirror.IK;
using UniRx;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    public class VMCPHandPose : IInitializable
    {
        private readonly ReactiveProperty<bool> _isActive = new ReactiveProperty<bool>(false);
        private readonly ReactiveProperty<IKDataStruct> _leftHandPose =
            new ReactiveProperty<IKDataStruct>(IKDataStruct.Empty);
        private readonly ReactiveProperty<IKDataStruct> _rightHandPose =
            new ReactiveProperty<IKDataStruct>(IKDataStruct.Empty);

        private readonly IVRMLoadable _vrmLoadable;
        private Vector3 _defaultHipOffset = Vector3.up;
        private bool _hasModel;

        public VMCPHandPose(IVRMLoadable vrmLoadable, IKTargetTransforms ikTargets)
        {
            _vrmLoadable = vrmLoadable;
        }
        
        public void Initialize()
        {
            _vrmLoadable.VrmLoaded += info =>
            {
                //NOTE: ロードした瞬間は原点に立ってるはず…という前提に基づく
                _defaultHipOffset = info.animator.GetBoneTransform(HumanBodyBones.Hips).position;
                _hasModel = true;
            };

            _vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _defaultHipOffset = Vector3.up;
            };
        }

        public IReadOnlyReactiveProperty<bool> IsActive => _isActive;
        public IReadOnlyReactiveProperty<IKDataStruct> LeftHandPose => _leftHandPose;
        public IReadOnlyReactiveProperty<IKDataStruct> RightHandPose => _rightHandPose;

        public void SetActive(bool active) => _isActive.Value = active;

        public void SetLeftHandPose(Vector3 position, Quaternion rotation)
            => _leftHandPose.Value = new IKDataStruct(position, rotation);
        
        public void SetRightHandPose(Vector3 position, Quaternion rotation)
            => _rightHandPose.Value = new IKDataStruct(position, rotation);

        public void SetLeftHandPoseOnHips(Vector3 position, Quaternion rotation)
        {
            _leftHandPose.Value = new IKDataStruct(_defaultHipOffset + position, rotation);
        }

        public void SetRightHandPoseOnHips(Vector3 position, Quaternion rotation)
        {
            _rightHandPose.Value = new IKDataStruct(_defaultHipOffset + position, rotation);
        }
    }
}

