using Baku.VMagicMirror.VMCP;
using R3;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    public class VMCPHandPose : IInitializable
    {
        private readonly ReactiveProperty<bool> _isActive = new ReactiveProperty<bool>(false);
        private readonly ReactiveProperty<bool> _isConnected = new ReactiveProperty<bool>(false);
        private readonly ReactiveProperty<IKDataStruct> _leftHandPose =
            new ReactiveProperty<IKDataStruct>(IKDataStruct.Empty);
        private readonly ReactiveProperty<IKDataStruct> _rightHandPose =
            new ReactiveProperty<IKDataStruct>(IKDataStruct.Empty);

        private readonly IVRMLoadable _vrmLoadable;
        private readonly VMCPBasedFingerSetter _fingerSetter; 
        private Vector3 _defaultHipOffset = Vector3.up;
        private bool _hasModel;

        public VMCPHandPose(
            IVRMLoadable vrmLoadable,
            VMCPBasedFingerSetter fingerSetter
            )
        {
            _vrmLoadable = vrmLoadable;
            _fingerSetter = fingerSetter;
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
        public IReadOnlyReactiveProperty<bool> IsConnected => _isConnected;
        public IReadOnlyReactiveProperty<IKDataStruct> LeftHandPose => _leftHandPose;
        public IReadOnlyReactiveProperty<IKDataStruct> RightHandPose => _rightHandPose;
        public VMCPBasedHumanoid Humanoid { get; private set; }

        public void ApplyFingerLocalPose()
        {
            if (Humanoid != null)
            {
                _fingerSetter.Set(Humanoid);
            }
        }

        public void SetActive(bool active)
        {
            _isActive.Value = active;
            if (!active)
            {
                _isConnected.Value = false;
            }
        }

        public void SetConnected(bool connected) => _isConnected.Value = connected;

        public void SetHumanoid(VMCPBasedHumanoid humanoid) => Humanoid = humanoid;

        public void SetLeftHandPoseOnHips(Vector3 position, Quaternion rotation)
            => _leftHandPose.Value = new IKDataStruct(_defaultHipOffset + position, rotation);

        public void SetRightHandPoseOnHips(Vector3 position, Quaternion rotation)
            => _rightHandPose.Value = new IKDataStruct(_defaultHipOffset + position, rotation);
    }
}

