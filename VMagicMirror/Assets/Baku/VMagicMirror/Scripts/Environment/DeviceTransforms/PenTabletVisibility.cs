using UnityEngine;

namespace Baku.VMagicMirror
{
    public class PenTabletVisibility : DeviceVisibilityBase
    {
        [SerializeField] private PenController pen = null;
        
        protected override void OnStart()
        {
            SetVisibility(false);
        }

        protected override void OnRendererEnableUpdated(bool enable)
        {
            pen.SetDeviceVisibility(enable);
        }
    }
}
