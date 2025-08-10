using Baku.VMagicMirror.VMCP;
using R3;

namespace Baku.VMagicMirror
{
    public class VMCPLowerBodyPose
    {
        private readonly ReactiveProperty<bool> _isActive = new(false);
        private readonly ReactiveProperty<bool> _isConnected = new(false);

        public IReadOnlyReactiveProperty<bool> IsActive => _isActive;
        public IReadOnlyReactiveProperty<bool> IsConnected => _isConnected;
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

