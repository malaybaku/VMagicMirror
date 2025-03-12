using System;
using System.IO;
using VMagicMirror.Buddy;

namespace Baku.VMagicMirror.Buddy.Api
{
    public class GlbApi : IGlb
    {
        private readonly string _baseDir;
        private readonly BuddyGlbInstance _instance;
        private readonly BuddyLogger _logger;
        private string BuddyId => _instance.BuddyId;

        public GlbApi(string baseDir, BuddyGlbInstance instance, BuddyLogger logger)
        {
            _baseDir = baseDir;
            _instance = instance;
            _logger = logger;
            Transform = new Transform3D(instance.GetTransform3D());
        }

        public ITransform3D Transform { get; }

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

        private void Try(Action act) => ApiUtils.Try(BuddyId, _logger, act);
        private T Try<T>(Func<T> func, T valueWhenFailed = default) => ApiUtils.Try(BuddyId, _logger, func, valueWhenFailed);
    }
}