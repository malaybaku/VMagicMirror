using VMagicMirror.Buddy;

namespace Baku.VMagicMirror.Buddy.Api
{
    public class Sprite3DApi : ISprite3D
    {
        private readonly BuddySprite3DInstance _instance;
        private readonly BuddyLogger _logger;
        private BuddyFolder BuddyFolder { get; }

        public Sprite3DApi(BuddyFolder buddyFolder, BuddySprite3DInstance instance, BuddyLogger logger)
        {
            BuddyFolder = buddyFolder;
            _instance = instance;
            _logger = logger;
            Transform = new Transform3D(instance.Transform3DInstance);
        }

        public override string ToString() => nameof(ISprite3D);
        
        public ITransform3D Transform { get; }

        public void Preload(string path) => ApiUtils.Try(BuddyFolder, _logger, () =>
        {
            var fullPath = ApiUtils.GetAssetFullPath(BuddyFolder, path);
            _instance.Preload(fullPath);
        });

        public void Show(string path) => ApiUtils.Try(BuddyFolder, _logger, () =>
        {
            var fullPath = ApiUtils.GetAssetFullPath(BuddyFolder, path);
            _instance.Show(fullPath);
        });
        
        public void ShowPreset(string name) => ApiUtils.Try(BuddyFolder, _logger, () =>
        {
            _instance.ShowPreset(name);
        });

        public void Hide() => _instance.SetActive(false);
    }
}
