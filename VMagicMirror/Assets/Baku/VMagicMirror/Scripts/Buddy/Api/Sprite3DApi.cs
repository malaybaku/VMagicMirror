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
        }

        private readonly string _baseDir;
        private readonly string _buddyId;
        private readonly BuddySprite3DInstance _instance;
        
        public Vector3 LocalPosition
        {
            get => _instance.LocalPosition.ToApiValue();
            set => _instance.LocalPosition = value.ToEngineValue();
        }

        public Quaternion LocalRotation
        {
            get => _instance.LocalRotation.ToApiValue();
            set => _instance.LocalRotation = value.ToEngineValue();
        }

        public Vector3 LocalScale
        {
            get => _instance.LocalScale.ToApiValue();
            set => _instance.LocalScale = value.ToEngineValue();
        }

        public Vector3 GetPosition() => _instance.GetWorldPosition().ToApiValue();
        public Quaternion GetRotation() => _instance.GetWorldRotation().ToApiValue();
        public void SetPosition(Vector3 position) => _instance.SetWorldPosition(position.ToEngineValue());
        public void SetRotation(Quaternion rotation) => _instance.SetWorldRotation(rotation.ToEngineValue());

        public void SetParent(ITransform3DApi parent)
        {
            var parentInstance = ((Transform3DApi)parent).GetInstance();
            _instance.SetParent(parentInstance);
        }

        public void Preload(string path)
        {
            var fullPath = Path.Combine(_baseDir, path);
            _instance.Preload(fullPath);
        }

        public void Show(string path)
        {
            var fullPath = Path.Combine(_baseDir, path);
            _instance.Show(fullPath);
        }

        public void Hide() => _instance.SetActive(false);
    }
}
