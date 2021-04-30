namespace Baku.VMagicMirror
{
    public class PenTabletVisibility : DeviceVisibilityBase
    {
        protected override void OnStart()
        {
            SetVisibility(false);
        }
    }
}
