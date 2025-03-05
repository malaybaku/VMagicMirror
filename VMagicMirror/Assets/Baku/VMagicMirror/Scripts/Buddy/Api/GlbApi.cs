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
            _transform = new Transform3D(instance.GetTransform3D());
        }

        private readonly string _baseDir;
        private readonly string _buddyId;
        private readonly BuddyGlbInstance _instance;

        private readonly Transform3D _transform;
        public ITransform3D Transform => _transform;

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

        public void RunAnimation(string name, bool loop) => Try(() => _instance.RunAnimation(name, loop, true));
        public void StopAnimation() => Try(() => _instance.StopAnimation());

        private void Try(Action act) => ApiUtils.Try(_buddyId, act);
        private T Try<T>(Func<T> func, T valueWhenFailed = default) => ApiUtils.Try(_buddyId, func, valueWhenFailed);
    }
}
