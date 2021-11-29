namespace Baku.VMagicMirrorConfig
{
    /// <summary> ヘルプ用のリンク類を処理するビューモデル </summary>
    public class HelpViewModel : ViewModelBase
    {
        private readonly UpdateChecker _updateChecker = new UpdateChecker();

        private ActionCommand? _openManualUrlCommand;
        public ActionCommand OpenManualUrlCommand
            => _openManualUrlCommand ??= new ActionCommand(OpenManualUrl);

        private ActionCommand? _openStandardDownloadUrlCommand;
        public ActionCommand OpenStandardDownloadUrlCommand
            => _openStandardDownloadUrlCommand ??= new ActionCommand(OpenStandardDownloadUrl);

        private ActionCommand? _openFullDownloadUrlCommand;
        public ActionCommand OpenFullDownloadUrlCommand
            => _openFullDownloadUrlCommand ??= new ActionCommand(OpenFullDownloadUrl);

        private ActionCommand? _openFanboxUrlCommand;
        public ActionCommand OpenFanboxUrlCommand
            => _openFanboxUrlCommand ??= new ActionCommand(OpenFanboxUrl);

        private ActionCommand? _showLicenseCommand;
        public ActionCommand ShowLicenseCommand
            => _showLicenseCommand ??= new ActionCommand(() => new LicenseWindow().ShowDialog());

        private ActionCommand? _checkUpdateCommand;
        public ActionCommand CheckUpdateCommand
            => _checkUpdateCommand ??= new ActionCommand(CheckUpdate);

        private void OpenManualUrl()
        {
            string url = LocalizedString.GetString("URL_help_top");
            UrlNavigate.Open(url);
        }

        private void OpenStandardDownloadUrl() => UrlNavigate.Open("https://baku-dreameater.booth.pm/items/1272298");
        private void OpenFullDownloadUrl() => UrlNavigate.Open("https://baku-dreameater.booth.pm/items/3064040");
        private void OpenFanboxUrl() => UrlNavigate.Open("https://baku-dreameater.fanbox.cc/");

        private async void CheckUpdate()
        {
            await _updateChecker.RunAsync(false);
        }
    }
}
