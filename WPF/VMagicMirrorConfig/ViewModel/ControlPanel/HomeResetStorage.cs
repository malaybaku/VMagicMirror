namespace Baku.VMagicMirrorConfig.ViewModel
{
    internal sealed class HomeResetStorage : IHomeResetStorage
    {
        private readonly SaveFileManager _saveFileManager;
        private readonly PreferenceFileManager _preferenceFileManager;

        public HomeResetStorage(SaveFileManager saveFileManager, PreferenceFileManager preferenceFileManager)
        {
            _saveFileManager = saveFileManager;
            _preferenceFileManager = preferenceFileManager;
        }

        public void DeleteAutoSaveFile()
            => _saveFileManager.SettingFileIo.DeleteSetting(SpecialFilePath.AutoSaveSettingFilePath);

        public void DeletePreferenceSaveFile()
            => _preferenceFileManager.DeleteSaveFile();
    }
}
