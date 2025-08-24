using Baku.VMagicMirror.VMCP;
using R3;

namespace Baku.VMagicMirror
{
    public class VMCPLowerBodyPose
    {
        private readonly ReactiveProperty<bool> _isActive = new(false);
        private readonly ReactiveProperty<bool> _isConnected = new(false);

        public ReadOnlyReactiveProperty<bool> IsActive => _isActive;
        public ReadOnlyReactiveProperty<bool> IsConnected => _isConnected;
        public VMCPBasedHumanoid Humanoid { get; private set; }

        public void SetActive(VMCPBasedHumanoid humanoid)
        {
            Humanoid = humanoid;
            SetActiveInternal(true);
        }

        public void SetInactive()
        {
            SetActiveInternal(false);
            Humanoid = null;
        }

        public void SetConnected(bool connected) => _isConnected.Value = connected;
        
        private void SetActiveInternal(bool active)
        {
            _isActive.Value = active;
            if (!active)
            {
                _isConnected.Value = false;
            }
        }
    }
}

