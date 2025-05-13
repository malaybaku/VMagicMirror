using System.Threading.Tasks;
using VMagicMirror.Buddy;

namespace Baku.VMagicMirror.Buddy.Api
{
    public class VrmAnimationApi : IVrmAnimation
    {
        private readonly BuddyFolder _buddyFolder;
        private readonly BuddyVrmAnimationInstance _instance;
        private readonly BuddyLogger _logger;

        internal BuddyVrmAnimationInstance Instance => _instance;
        
        public VrmAnimationApi(
            BuddyFolder buddyFolder,
            BuddyVrmAnimationInstance instance,
            BuddyLogger logger)
        {
            _buddyFolder = buddyFolder;
            _instance = instance;
            _logger = logger;
        }

        public async Task LoadAsync(string path) => await ApiUtils.TryAsync(
            _buddyFolder,
            _logger,
            async () =>
            {
                //TODO: パス不正とかファイルが見つからないとかの場合に1回だけエラーが出したい
                // Sprite2Dの処理を使い回せるとgood
                var fullPath = ApiUtils.GetAssetFullPath(_buddyFolder, path);
                await _instance.LoadAsync(fullPath);
            });

        public bool IsLoaded => _instance.IsLoaded;
        
        public float GetLength() => _instance.GetLength();
    }
}
