using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public class VMCPHandPose
    {
        private readonly ReactiveProperty<bool> _isActive = new ReactiveProperty<bool>(false);
        private readonly ReactiveProperty<IKDataStruct> _leftHandPose =
            new ReactiveProperty<IKDataStruct>(IKDataStruct.Empty);
        private readonly ReactiveProperty<IKDataStruct> _rightHandPose =
            new ReactiveProperty<IKDataStruct>(IKDataStruct.Empty);

        public IReadOnlyReactiveProperty<bool> IsActive => _isActive;
        public IReadOnlyReactiveProperty<IKDataStruct> LeftHandPose => _leftHandPose;
        public IReadOnlyReactiveProperty<IKDataStruct> RightHandPose => _rightHandPose;

        public void SetActive(bool active) => _isActive.Value = active;

        public void SetLeftHandPose(Vector3 position, Quaternion rotation)
            => _leftHandPose.Value = new IKDataStruct(position, rotation);
        
        public void SetRightHandPose(Vector3 position, Quaternion rotation)
            => _rightHandPose.Value = new IKDataStruct(position, rotation);
    }
}

