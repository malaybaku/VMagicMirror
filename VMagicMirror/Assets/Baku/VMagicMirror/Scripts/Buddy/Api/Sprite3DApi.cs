using System.IO;
using Baku.VMagicMirror.Buddy.Api.Interface;

namespace Baku.VMagicMirror.Buddy.Api
{
    public class Sprite3DApi : ISprite3DApi
    {
        public Sprite3DApi(string baseDir, string buddyId, BuddySprite3DInstance instance)
        {
            _baseDir = baseDir;
            _buddyId = buddyId;
            _instance = instance;
            _transform = new Transform3D(instance.Transform3DInstance);
        }

        private readonly string _baseDir;
        private readonly string _buddyId;
        private readonly BuddySprite3DInstance _instance;

        private readonly Transform3D _transform;
        public ITransform3D Transform => _transform;

        public void Preload(string path) => ApiUtils.Try(_buddyId, () =>
        {
            var fullPath = Path.Combine(_baseDir, path);
            _instance.Preload(fullPath);
        });

        public void Show(string path) => ApiUtils.Try(_buddyId, () =>
        {
            var fullPath = Path.Combine(_baseDir, path);
            _instance.Show(fullPath);
        });

        public void Hide() => _instance.SetActive(false);
    }
}
