namespace Baku.VMagicMirrorConfig.ViewModel
{
    internal interface ISaveLoadDataService
    {
        int FileCount { get; }
        int FocusedFileIndex { get; }
        bool CheckFileExist(int index);
        void LoadSetting(int index, bool loadCharacter, bool loadNonCharacter, bool fromAutomation);
        void SaveCurrentSetting(int index);
    }
}
