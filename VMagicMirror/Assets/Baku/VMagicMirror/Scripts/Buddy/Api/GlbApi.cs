using System;
using System.IO;
using Baku.VMagicMirror.Buddy.Api.Interface;

namespace Baku.VMagicMirror.Buddy.Api
{
    public class GlbApi : IGlbApi
    {
        public GlbApi(string baseDir, string buddyId, BuddyGlbInstance instance)
        {
            _baseDir = baseDir;
            _buddyId = buddyId;
            _instance = instance;
        }

        private readonly string _baseDir;
        private readonly string _buddyId;
        private readonly BuddyGlbInstance _instance;
        
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

        public void Load(string path) => Try(() =>
        {
            var fullPath = Path.Combine(_baseDir, path);
            _instance.Load(fullPath);
        });

        public void Show() => _instance.Show();
        public void Hide() => _instance.Hide();

        public string[] GetAnimationNames() => Try(
            () => _instance.GetAnimationNames(), 
            Array.Empty<string>()
            );

        public void RunAnimation(string name) => Try(() => _instance.RunAnimation(name, false, true));
        public void StopAnimation() => Try(() => _instance.StopAnimation());

        private void Try(Action act) => ApiUtils.Try(_buddyId, act);
        private T Try<T>(Func<T> func, T valueWhenFailed = default) => ApiUtils.Try(_buddyId, func, valueWhenFailed);
    }
}
