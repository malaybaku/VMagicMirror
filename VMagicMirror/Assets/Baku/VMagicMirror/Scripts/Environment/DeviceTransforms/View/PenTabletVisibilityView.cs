namespace Baku.VMagicMirror
{
    public class PenTabletVisibilityView : DeviceVisibilityBase
    {
        protected override void OnStart()
        {
            //NOTE: 明示的に非表示にすることで、起動直後に見えてしまうのを防ぐ
            MainRenderer.enabled = false;
            SetVisibility(false);
        }
    }
}
