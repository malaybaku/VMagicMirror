using UnityEngine;

namespace Baku.VMagicMirror
{
    public class PenTabletVisibility : DeviceVisibilityBase
    {
        public PenController PenController { get; set; }
        
        protected override void OnStart()
        {
            SetVisibility(false);
        }

        protected override void OnRendererEnableUpdated(bool enable)
        {
            PenController.SetDeviceVisibility(enable);
        }
    }
}
