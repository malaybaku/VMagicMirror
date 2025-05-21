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
            var result = _instance.Load(fullPath);
            HandleTextureLoadResult(fullPath, result);
        });

        public void Show(string path) => ApiUtils.Try(BuddyFolder, _logger, () =>
        {
            var fullPath = ApiUtils.GetAssetFullPath(BuddyFolder, path);
            var result = _instance.Show(fullPath);
            HandleTextureLoadResult(fullPath, result);
        });
        
        public void ShowPreset(string name) => ApiUtils.Try(BuddyFolder, _logger, () =>
        {
            var result = _instance.ShowPreset(name);
            HandlePresetTextureLoadResult(name, result);
        });

        public void Hide() => ApiUtils.Try(BuddyFolder, _logger, () =>
        {
            _instance.Hide();
        });
        
        private void HandleTextureLoadResult(string fullPath, TextureLoadResult loadResult)
        {
            if (loadResult is TextureLoadResult.FailureFileNotFound)
            {
                _logger.Log(BuddyFolder, "Specified file does not exist: " + fullPath, BuddyLogLevel.Fatal);
            }
        }
        
        private void HandlePresetTextureLoadResult(string name, TextureLoadResult result)
        {
            if (result is TextureLoadResult.FailureFileNotFound)
            {
                _logger.Log(BuddyFolder, "Specified preset does not exist: " + name, BuddyLogLevel.Fatal);
            }
        }
    }
}
