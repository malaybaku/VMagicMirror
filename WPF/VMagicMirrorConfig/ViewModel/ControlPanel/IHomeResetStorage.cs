namespace Baku.VMagicMirrorConfig.ViewModel
{
    internal interface IHomeResetStorage
    {
        void DeleteAutoSaveFile();
        void DeletePreferenceSaveFile();
    }
}
