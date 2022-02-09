namespace Baku.VMagicMirrorConfig
{
    public class AccessoryItemNameViewModel : ViewModelBase
    {
        public AccessoryItemNameViewModel(string fileId, string displayName)
        {
            FileId = fileId;
            DisplayName = displayName;
        }

        public string FileId { get; }

        //NOTE: 実装の都合上ViewModelが発生してからなくなるまでの間は表示名が不変なので、単に値を持っておけばOK
        public string DisplayName { get; }

        public static AccessoryItemNameViewModel None { get; }
            = new AccessoryItemNameViewModel("", "(None)");
    }
}
