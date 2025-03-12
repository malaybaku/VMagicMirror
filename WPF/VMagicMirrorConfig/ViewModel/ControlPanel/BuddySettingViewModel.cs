using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    // Buddy全体の設定のVM。個別のBuddy設定に加えて、全Buddyに共通の設定もここに入る
    public class BuddySettingViewModel : ViewModelBase
    {
        public BuddySettingViewModel() : this(
            ModelResolver.Instance.Resolve<BuddySettingModel>(),
            ModelResolver.Instance.Resolve<LayoutSettingModel>(),
            ModelResolver.Instance.Resolve<BuddySettingsSender>())
        {
        }

        internal BuddySettingViewModel(
            BuddySettingModel model,
            LayoutSettingModel layoutSettingModel,
            BuddySettingsSender buddySettingsSender)
        {
            _model = model;
            _layoutSettingModel = layoutSettingModel;
            _buddySettingsSender = buddySettingsSender;
            Items = new ReadOnlyObservableCollection<BuddyItemViewModel>(_items);

            OpenBuddyFolderCommand = new ActionCommand(OpenBuddyFolder);
            OpenDocUrlCommand = new ActionCommand(OpenDocUrl);
            ReloadAllCommand = new ActionCommand(() => _model.ReloadAll());

            if (!IsInDesignMode)
            {
                WeakEventManager<BuddySettingModel, EventArgs>
                    .AddHandler(_model, nameof(_model.BuddiesReloaded), OnBuddiesReloaded);
                WeakEventManager<BuddySettingModel, BuddyDataEventArgs>
                    .AddHandler(_model, nameof(_model.BuddyUpdated), OnBuddyUpdated);
                WeakEventManager<BuddySettingModel, BuddyLogMessageEventArgs>
                    .AddHandler(_model, nameof(_model.ReceivedLog), OnBuddyLogReceived);
                OnBuddiesReloaded();
            }
        }

        private readonly BuddySettingModel _model;
        private readonly LayoutSettingModel _layoutSettingModel;
        private readonly BuddySettingsSender _buddySettingsSender;

        public RProperty<bool> EnableDeviceFreeLayout => _layoutSettingModel.EnableDeviceFreeLayout;

        public string MainAvatarOutputActiveLabel
        {
            get
            {
                if (IsInDesignMode)
                {
                    return "メインアバターAPIを使用";
                }

                return FeatureLocker.FeatureLocked
                    ? LocalizedString.GetString("Buddy_MainAvatarOutputActive_StandardEdition")
                    : LocalizedString.GetString("Buddy_MainAvatarOutputActive_FullEdition");
            }
        }

        public RProperty<bool> MainAvatarOutputActive => _model.MainAvatarOutputActive;
        public RProperty<bool> DeveloperModeActive => _model.DeveloperModeActive;
        public RProperty<int> DeveloperModeLogLevel => _model.DeveloperModeLogLevel;


        private readonly ObservableCollection<BuddyItemViewModel> _items = new();
        public ReadOnlyObservableCollection<BuddyItemViewModel> Items { get; }        

        public ActionCommand OpenBuddyFolderCommand { get; }
        public ActionCommand OpenDocUrlCommand { get; }
        public ActionCommand ReloadAllCommand { get; }

        private void OnBuddiesReloaded(object? sender, EventArgs e) => OnBuddiesReloaded();
        private void OnBuddiesReloaded()
        {
            foreach(var item in _items)
            {
                item.ReloadRequested -= ReloadBuddy;
            }
            _items.Clear();

            foreach (var buddy in _model.Buddies)
            {
                var item = new BuddyItemViewModel(_buddySettingsSender, buddy);
                item.ReloadRequested += ReloadBuddy;
                _items.Add(item);
            }
        }

        private void OnBuddyUpdated(object? sender, BuddyDataEventArgs e)
        {
            _items[e.Index].ReloadRequested -= ReloadBuddy;
            _items.RemoveAt(e.Index);

            var item = new BuddyItemViewModel(_buddySettingsSender, e.BuddyData);
            item.ReloadRequested += ReloadBuddy;
            _items.Insert(e.Index, item);
        }

        private void OnBuddyLogReceived(object? sender, BuddyLogMessageEventArgs e)
        {
            // 例えば現在のログレベルが Error のときに Warning が飛んできたら無視する
            if (e.BuddyLogLevel > _model.CurrentLogLevel)
            {
                return;
            }

            _items.FirstOrDefault(_items => _items.BuddyId == e.BuddyId)
                ?.EnqueueLogMessage(e.Message);
        }

        internal void ReloadBuddy(BuddyData buddy) => _model.ReloadBuddy(buddy);

        private void OpenBuddyFolder()
        {
            if (Directory.Exists(SpecialFilePath.BuddyDir))
            {
                Process.Start(new ProcessStartInfo(SpecialFilePath.BuddyDir)
                {
                    UseShellExecute = true,
                });
            }
        }

        private void OpenDocUrl() => UrlNavigate.Open(LocalizedString.GetString("URL_docs_buddy"));
    }

    /// <summary> 単一のBuddyの設定に対応するVM </summary>
    public class BuddyItemViewModel
    {
        private const int LogMessageMaxCount = 10;

        private readonly BuddyData _buddyData;

        internal BuddyItemViewModel(BuddySettingsSender settingsSender, BuddyData buddyData)
        {
            _buddyData = buddyData;
            LogMessages = new ReadOnlyObservableCollection<string>(_logMessages);

            ReloadCommand = new ActionCommand(() => ReloadRequested?.Invoke(_buddyData));
            ResetSettingsCommand = new ActionCommand(ResetSettingsAsync);
            ClearLogCommand = new ActionCommand(ClearLog);
            OpenLogFileCommand = new ActionCommand(OpenLogFile);

            Properties = buddyData.Properties
                .Select(p => new BuddyPropertyViewModel(settingsSender, buddyData.Metadata, p))
                .ToArray();
        }

        public event Action<BuddyData>? ReloadRequested;

        public RProperty<bool> IsActive => _buddyData.IsActive;

        public ActionCommand ReloadCommand { get; }
        public ActionCommand ResetSettingsCommand { get; }

        public ActionCommand ClearLogCommand { get; }
        public ActionCommand OpenLogFileCommand { get; }

        public IReadOnlyList<BuddyPropertyViewModel> Properties { get; }

        public string BuddyId => FolderName;
        public string FolderName => _buddyData.Metadata.FolderName;
        public string DisplayName => _buddyData.Metadata.DisplayName;

        // TODO: info以下 / warn / error以上 くらいで3色に分けたくなりそう。stringの書式ベースでView側で勝手にやるでもいいが
        private readonly ObservableCollection<string> _logMessages = [];
        public ReadOnlyObservableCollection<string> LogMessages { get; }

        public void EnqueueLogMessage(BuddyLogMessage message)
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                _logMessages.Add(message.Message);
                while (_logMessages.Count > LogMessageMaxCount)
                {
                    _logMessages.RemoveAt(0);
                }
            });
        }

        private async void ResetSettingsAsync()
        {
            //TODO: localize
            var result = await MessageBoxWrapper.Instance.ShowAsync(
                "サブキャラ設定のリセット",
                $"サブキャラ「{DisplayName}」の設定をデフォルト値に戻しますか？",
                MessageBoxWrapper.MessageBoxStyle.OKCancel
                )
                .ConfigureAwait(true);

            if (!result)
            {
                return;
            }

            // プロパティ変更の通知が個別でバシバシ飛ぶのが若干ダサいっちゃダサい
            // この挙動が気になる場合、明示的に設定の同期をオフにして書き換えてから一括送信…としてもよい
            foreach(var property in Properties)
            {
                property.ResetToDefault();
            }
        }

        // NOTE: Enqueueとのタイミングを考えてBeginInvokeしてもいいが、まあいい加減に…
        private void ClearLog() => _logMessages.Clear();

        private void OpenLogFile()
        {
            var filePath = SpecialFilePath.GetBuddyLogFilePath(BuddyId);
            if (!File.Exists(filePath))
            {
                var snackbarMessage = string.Format(
                    LocalizedString.GetString("Snackbar_Buddy_LogFileNotFound_Format"),
                    BuddyId
                    );
                SnackbarWrapper.Enqueue(snackbarMessage);
                return;
            }

            Process.Start(new ProcessStartInfo(filePath)
            {
                UseShellExecute = true,
            });
        }
    }
}
