using System.IO;
using Baku.VMagicMirror.Buddy.Api.Interface;

namespace Baku.VMagicMirror.Buddy.Api
{
    public class Sprite3DApi : ISprite3D
    {
        private readonly string _baseDir;
        private readonly BuddySprite3DInstance _instance;
        private string BuddyId => _instance.BuddyId; 

        public Sprite3DApi(string baseDir, BuddySprite3DInstance instance)
        {
            _baseDir = baseDir;
            _instance = instance;
            Transform = new Transform3D(instance.Transform3DInstance);
        }

        public ITransform3D Transform { get; }

        public void Preload(string path) => ApiUtils.Try(BuddyId, () =>
        {
            var fullPath = Path.Combine(_baseDir, path);
            _instance.Preload(fullPath);
        });

        public void Show(string path) => ApiUtils.Try(BuddyId, () =>
        {
            var fullPath = Path.Combine(_baseDir, path);
            _instance.Show(fullPath);
        });

        public void Hide() => _instance.SetActive(false);
    }
}
