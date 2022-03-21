namespace Baku.VMagicMirrorConfig.ViewModel
{
    public class UpdateNotificationViewModel : ViewModelBase
    {
        public UpdateNotificationViewModel(UpdateCheckResult modelData)
        {
            _modelData = modelData;

            CurrentVersion = AppConsts.AppVersion.ToString();
            LatestVersion = modelData.Version.ToString();
            ReleaseDate = string.IsNullOrEmpty(modelData.ReleaseNote.DateString) 
                ? ""
                : " (" + modelData.ReleaseNote.DateString + ")";
            
            GetLatestVersionCommand = new ActionCommand(() => SetResult(UpdateDialogResult.GetLatestVersion));
            AskMeLaterCommand = new ActionCommand(() => SetResult(UpdateDialogResult.AskMeLater));
            SkipThisVersionCommand = new ActionCommand(() => SetResult(UpdateDialogResult.SkipThisVersion));
        }
        private readonly UpdateCheckResult _modelData;

        //NOTE: この値はModel層があとで見に来る
        public UpdateDialogResult Result { get; private set; } = UpdateDialogResult.AskMeLater;

        public string CurrentVersion { get; }
        public string LatestVersion { get; }
        public string ReleaseDate { get; }

        public string ReleaseNote => (LanguageSelector.Instance.LanguageName == LanguageSelector.LangNameJapanese)
            ? _modelData.ReleaseNote.JapaneseNote
            : _modelData.ReleaseNote.EnglishNote;

        public ActionCommand GetLatestVersionCommand { get; }
        public ActionCommand AskMeLaterCommand { get; }
        public ActionCommand SkipThisVersionCommand { get; }

        private bool? _dialogResult = null;
        public bool? DialogResult
        {
            get => _dialogResult;
            private set
            {
                if (_dialogResult != value)
                {
                    _dialogResult = value;
                    RaisePropertyChanged();
                }
            }
        }
        
        private void SetResult(UpdateDialogResult result)
        {
            Result = result;
            DialogResult = true;
        }
    }
}
