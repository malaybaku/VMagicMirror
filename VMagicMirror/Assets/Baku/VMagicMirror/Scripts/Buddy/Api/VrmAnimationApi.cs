using System.IO;
using System.Threading.Tasks;
using VMagicMirror.Buddy;

namespace Baku.VMagicMirror.Buddy.Api
{
    public class VrmAnimationApi : IVrmAnimation
    {
        private readonly string _baseDir;
        private readonly BuddyVrmAnimationInstance _instance;

        internal BuddyVrmAnimationInstance Instance => _instance;
        
        public VrmAnimationApi(string baseDir, BuddyVrmAnimationInstance instance)
        {
            _baseDir = baseDir;
            _instance = instance;
        }

        public async Task LoadAsync(string path)
        {
            //TODO: パス不正とかファイルが見つからないとかの場合に1回だけエラーが出したい
            // Sprite2Dの処理を使い回せるとgood
            var fullPath = Path.Combine(_baseDir, path);
            await _instance.LoadAsync(fullPath);
        }

        public bool IsLoaded => _instance.IsLoaded;
        
        public float GetLength() => _instance.GetLength();
    }
}
