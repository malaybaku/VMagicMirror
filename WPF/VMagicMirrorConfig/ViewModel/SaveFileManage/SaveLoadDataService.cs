namespace Baku.VMagicMirrorConfig.ViewModel
{
    internal sealed class SaveLoadDataService : ISaveLoadDataService
    {
        private readonly SaveFileManager _model;

        public SaveLoadDataService(SaveFileManager model)
        {
            _model = model;
        }

        public int FileCount => SaveFileManager.FileCount;
        public int FocusedFileIndex => _model.FocusedFileIndex;
        public bool CheckFileExist(int index) => _model.CheckFileExist(index);
        public void LoadSetting(int index, bool loadCharacter, bool loadNonCharacter, bool fromAutomation)
            => _model.LoadSetting(index, loadCharacter, loadNonCharacter, fromAutomation);
        public void SaveCurrentSetting(int index) => _model.SaveCurrentSetting(index);
    }
}
