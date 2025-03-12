using System.IO;
using VMagicMirror.Buddy;

namespace Baku.VMagicMirror.Buddy.Api
{
    public class Sprite3DApi : ISprite3D
    {
        private readonly string _baseDir;
        private readonly BuddySprite3DInstance _instance;
        private readonly BuddyLogger _logger;
        private string BuddyId => _instance.BuddyId; 

        public Sprite3DApi(string baseDir, BuddySprite3DInstance instance, BuddyLogger logger)
        {
            _baseDir = baseDir;
            _instance = instance;
            _logger = logger;
            Transform = new Transform3D(instance.Transform3DInstance);
        }

        public ITransform3D Transform { get; }

        public void Preload(string path) => ApiUtils.Try(BuddyId, _logger, () =>
        {
            var fullPath = Path.Combine(_baseDir, path);
            _instance.Preload(fullPath);
        });

        public void Show(string path) => ApiUtils.Try(BuddyId, _logger, () =>
        {
            var fullPath = Path.Combine(_baseDir, path);
            _instance.Show(fullPath);
        });

        public void Hide() => _instance.SetActive(false);
    }
}
