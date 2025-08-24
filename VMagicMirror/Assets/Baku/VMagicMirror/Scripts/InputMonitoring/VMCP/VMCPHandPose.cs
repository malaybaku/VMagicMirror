using Baku.VMagicMirror.VMCP;
using R3;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    public class VMCPHandPose
    {
        private readonly ReactiveProperty<bool> _isActive = new(false);
        private readonly ReactiveProperty<bool> _isConnected = new(false);
        private readonly VMCPBasedFingerSetter _fingerSetter; 

        public VMCPHandPose(VMCPBasedFingerSetter fingerSetter)
        {
            _fingerSetter = fingerSetter;
        }

        public ReadOnlyReactiveProperty<bool> IsActive => _isActive;
        public ReadOnlyReactiveProperty<bool> IsConnected => _isConnected;
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
    }
}

