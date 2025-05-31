using Baku.VMagicMirrorConfig.Model;
using Baku.VMagicMirrorConfig.View;
using System.Text;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    /// <summary> ヘルプ用のリンク類を処理するビューモデル </summary>
    public class HelpViewModel : ViewModelBase
    {
        public HelpViewModel() : this(ModelResolver.Instance.Resolve<IMessageSender>())
        {
        }

        internal HelpViewModel(IMessageSender sender)
        {
            _sender = sender;
        }

        private readonly IMessageSender _sender;
        private readonly UpdateChecker _updateChecker = new();

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

        private void OpenManualUrl() => UrlNavigate.Open(LocalizedString.GetString("URL_help_top"));
        private void OpenStandardDownloadUrl() => UrlNavigate.Open("https://baku-dreameater.booth.pm/items/1272298");
        private void OpenFullDownloadUrl() => UrlNavigate.Open("https://baku-dreameater.booth.pm/items/3064040");
        private void OpenFanboxUrl() => UrlNavigate.Open("https://baku-dreameater.fanbox.cc/");
        private async void CheckUpdate() => await _updateChecker.RunAsync(false);


        // ここから下はデバッグ機能用に場所を間借りして実装している
        public bool IsDebugBuild => TargetEnvironmentChecker.CheckDevEnvFlagEnabled();

        private ActionCommand? _sendLargeDataCommand;
        public ActionCommand SendLargeDataCommand
            => _sendLargeDataCommand ??= new ActionCommand(SendLargeData);
        private void SendLargeData()
        {
            // NOTE: MMFで大きなデータの送受信が可能になっていることを検証するための機能。
            // - データサイズは2MB以上~10MB以下くらいで検証したい…というサイズ感
            // - ここで送る文字列の内訳 (a-zを10万回繰り返したものである) 自体はUnity側でも既知
            var sb = new StringBuilder();
            for (int i = 0; i < 100000; i++)
            {
                for (var c = 'a'; c <= 'z'; c++)
                {
                    sb.Append(c);
                }
            }

            _sender.SendMessage(MessageFactory.DebugSendLargeData(sb.ToString()));
        }
    }
}
