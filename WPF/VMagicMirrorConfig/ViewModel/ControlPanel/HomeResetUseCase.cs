using System.Threading.Tasks;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    internal sealed class HomeResetUseCase
    {
        private readonly AppQuitSetting _appQuitSetting;
        private readonly IHomeResetStorage _storage;
        private readonly IHomeResetInteraction _interaction;

        public HomeResetUseCase(
            AppQuitSetting appQuitSetting,
            IHomeResetStorage storage,
            IHomeResetInteraction interaction)
        {
            _appQuitSetting = appQuitSetting;
            _storage = storage;
            _interaction = interaction;
        }

        public async Task ExecuteAsync()
        {
            if (!await _interaction.ConfirmResetToDefaultAsync())
            {
                return;
            }

            // 「設定のデフォルト化 == 設定ファイルを消してオートセーブ無効で再起動」として扱う。
            _appQuitSetting.SkipAutoSaveAndRestart = true;
            _storage.DeleteAutoSaveFile();
            // オートセーブ以外の設定ファイルも削除する。
            _storage.DeletePreferenceSaveFile();
            _interaction.CloseMainWindow();
        }
    }
}
