using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
            OpenDeveloperModeDocUrlCommand = new ActionCommand(OpenDeveloperDocUrl);
            ReloadAllCommand = new ActionCommand(() => _model.ReloadAll());

            if (!IsInDesignMode)
            {
                WeakEventManager<BuddySettingModel, EventArgs>
                    .AddHandler(_model, nameof(_model.BuddiesReloaded), OnBuddiesReloaded);
                WeakEventManager<BuddySettingModel, BuddyDataEventArgs>
                    .AddHandler(_model, nameof(_model.BuddyUpdated), OnBuddyUpdated);
                WeakEventManager<BuddySettingModel, BuddyLogMessageEventArgs>
                    .AddHandler(_model, nameof(_model.ReceivedLog), OnBuddyLogReceived);

                _model.DeveloperModeActive.AddWeakEventHandler(OnModelDeveloperModeChanged);

                WeakEventManager<LanguageSelector, PropertyChangedEventArgs>.AddHandler(
                    LanguageSelector.Instance, 
                    nameof(LanguageSelector.PropertyChanged),
                    OnLanguageSelectorPropertyChanged
                    );

                OnBuddiesReloaded();
            }
        }

        private readonly BuddySettingModel _model;
        private readonly LayoutSettingModel _layoutSettingModel;
        private readonly BuddySettingsSender _buddySettingsSender;

        public RProperty<bool> EnableDeviceFreeLayout => _layoutSettingModel.EnableDeviceFreeLayout;

        public string InteractionApiEnabledLabel
        {
            get
            {
                if (IsInDesignMode)
                {
                    return "メインアバターAPIを使用";
                }

                return FeatureLocker.FeatureLocked
                    ? LocalizedString.GetString("Buddy_InteractionApiEnabled_StandardEdition")
                    : LocalizedString.GetString("Buddy_InteractionApiEnabled_FullEdition");
            }
        }

        public RProperty<bool> InteractionApiEnabled => _model.InteractionApiEnabled;
        public RProperty<bool> DeveloperModeActive => _model.DeveloperModeActive;
        public RProperty<int> DeveloperModeLogLevel => _model.DeveloperModeLogLevel;


        private readonly ObservableCollection<BuddyItemViewModel> _items = new();
        public ReadOnlyObservableCollection<BuddyItemViewModel> Items { get; }        

        public string[] AvailableLogLevelNames { get; } = [
            "Fatal",
            "Error",
            "Warning",
            "Info", 
            "Verbose",
        ];

        public ActionCommand OpenBuddyFolderCommand { get; }
        public ActionCommand OpenDocUrlCommand { get; }
        public ActionCommand OpenDeveloperModeDocUrlCommand { get; }
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
                item.SetDeveloperModeActive(DeveloperModeActive.Value);
                item.ReloadRequested += ReloadBuddy;
                _items.Add(item);
            }

            ApplyLanguage();
        }

        private void OnBuddyUpdated(object? sender, BuddyDataEventArgs e)
        {
            var buddyToRemove = _items[e.Index];
            buddyToRemove.ReloadRequested -= ReloadBuddy;
            _items.RemoveAt(e.Index);

            var item = new BuddyItemViewModel(_buddySettingsSender, e.BuddyData);
            // Expanderの状態を引き継ぐ: 再読み込みの前後でUIの見えが変化しすぎないようにするのが狙い
            item.ItemDetailIsVisible.Value = buddyToRemove.ItemDetailIsVisible.Value;
            item.SetDeveloperModeActive(DeveloperModeActive.Value);
            item.ReloadRequested += ReloadBuddy;
            _items.Insert(e.Index, item);

            item.ApplyLanguage(LanguageSelector.Instance.LanguageName == LanguageSelector.LangNameJapanese);
        }

        private void OnBuddyLogReceived(object? sender, BuddyLogMessageEventArgs e)
        {
            // 例えば現在のログレベルが Error のときに Warning が飛んできたら無視する
            if (e.BuddyLogLevel > _model.CurrentLogLevel)
            {
                return;
            }

            _items
                .FirstOrDefault(_items => _items.BuddyId.Equals(e.BuddyId))
                ?.EnqueueLogMessage(e.Message);
        }

        public void ReloadBuddy(BuddyData buddy) => _model.ReloadBuddy(buddy);

        private void OnModelDeveloperModeChanged(object? sender, PropertyChangedEventArgs args)
        {
            foreach(var item in _items)
            {
                item.SetDeveloperModeActive(DeveloperModeActive.Value);
            }
        }

        private void OnLanguageSelectorPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(LanguageSelector.LanguageName))
            {
                return;
            }

            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            var isJapanese = LanguageSelector.Instance.LanguageName == LanguageSelector.LangNameJapanese;
            foreach (var item in _items)
            {
                item.ApplyLanguage(isJapanese);
            }
        }

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
        private void OpenDeveloperDocUrl() => UrlNavigate.Open(LocalizedString.GetString("URL_docs_buddy_developer"));
    }

    /// <summary> 単一のBuddyの設定に対応するVM </summary>
    public class BuddyItemViewModel
    {
        private const int LogMessageMaxCount = 10;

        private readonly BuddyData _buddyData;

        internal BuddyItemViewModel(BuddySettingsSender settingsSender, BuddyData buddyData)
        {
            _buddyData = buddyData;
            LogMessages = new ReadOnlyObservableCollection<BuddyLogMessage>(_logMessages);

            ReloadCommand = new ActionCommand(() => ReloadRequested?.Invoke(_buddyData));
            ResetSettingsCommand = new ActionCommand(ResetSettingsAsync);

            CopyLogMessageCommand = new ActionCommand(CopyLogMessage);
            ClearLogCommand = new ActionCommand(ClearLog);
            OpenLogFileCommand = new ActionCommand(OpenLogFile);

            Properties = buddyData.Properties
                .Select(p => new BuddyPropertyViewModel(settingsSender, buddyData.Metadata, p))
                .ToArray();

            // NOTE: 普通のRxならもっとシンプルに書けるけど、まあコレでも困らないので…
            IsDeveloperMode.PropertyChanged += (_, __) =>
            {
                HasNonDeveloperError.Value = !IsDeveloperMode.Value && CurrentFatalError.Value != null;
                ShouldRestartToApplyDeveloperMode.Value =
                    IsDeveloperMode.Value && _buddyData.IsEnabledWithoutDeveloperMode.Value;
            };
            CurrentFatalError.PropertyChanged += (_, __) => HasNonDeveloperError.Value = !IsDeveloperMode.Value && CurrentFatalError.Value != null;

            _buddyData.IsEnabledWithoutDeveloperMode.AddWeakEventHandler(OnEnableWithDeveloperModeChanged);
        }

        public event Action<BuddyData>? ReloadRequested;

        public RProperty<bool> IsActive => _buddyData.IsActive;

        public RProperty<bool> IsDeveloperMode { get; } = new(false);

        public RProperty<bool> ShouldRestartToApplyDeveloperMode { get; } = new(false);

        // trueの場合、サブキャラのタイトルバー的な部分がエラー表示になる
        public RProperty<bool> HasError { get; } = new(false);

        // これがtrue、かつ開発者モードがオフの場合、非開発者向けのエラー表示を行う
        public RProperty<bool> HasNonDeveloperError { get; } = new(false);

        private BuddyLogLevel _mostSevereErrorLevel = BuddyLogLevel.Verbose;

        public ActionCommand ReloadCommand { get; }
        public ActionCommand ResetSettingsCommand { get; }

        public ActionCommand CopyLogMessageCommand { get; }
        public ActionCommand ClearLogCommand { get; }
        public ActionCommand OpenLogFileCommand { get; }

        public IReadOnlyList<BuddyPropertyViewModel> Properties { get; }

        // NOTE: デフォルトサブキャラではBuddyIdに ">" のprefixがついて ">Foo" みたいな文字列になり、UIに表示するには適さない(のでinternal)
        internal BuddyId BuddyId => _buddyData.Metadata.BuddyId;
        public string FolderName => _buddyData.Metadata.FolderName;
        public RProperty<string> DisplayName { get; } = new("");
        
        // TODO: info以下 / warn / error以上 くらいで3色に分けたくなりそう。stringの書式ベースでView側で勝手にやるでもいいが
        private readonly ObservableCollection<BuddyLogMessage> _logMessages = [];
        public ReadOnlyObservableCollection<BuddyLogMessage> LogMessages { get; }

        /// <summary> 非開発者に表示する想定の重大エラー </summary>
        public RProperty<BuddyLogMessage?> CurrentFatalError { get; } = new(null);

        /// <summary>
        /// NOTE: UI上のExpanderの開閉を覚えておくプロパティ。
        /// 通常は単にViewと同期しているだけの値だが、Buddy単体をリロードしたときはリロード前後でフラグの値を引き継ぐ
        /// </summary>
        public RProperty<bool> ItemDetailIsVisible { get; } = new(false);

        public void ApplyLanguage(bool isJapanese)
        {
            DisplayName.Value = _buddyData.Metadata.DisplayName.Get(isJapanese);
            foreach (var property in Properties)
            {
                property.ApplyLanguage(isJapanese);
            }
        }


        public void SetDeveloperModeActive(bool active)
        {
            IsDeveloperMode.Value = active;
            UpdateHasError();
        }


        public void EnqueueLogMessage(BuddyLogMessage message)
        {
            // NOTE: Fatal Errorはクリアしないと更新されない。
            // これはログがコロコロ変わるのを防ぐのと、最初のエラーが一番怪しいよねというヒューリスティックも踏まえた処置
            if (message.Level is BuddyLogLevel.Fatal &&
                (CurrentFatalError.Value == null || CurrentFatalError.Value.Level < BuddyLogLevel.Fatal))
            {
                CurrentFatalError.Value = message;
            }

            // NOTE: エラーのserverityはClearしない限りは下がらない
            _mostSevereErrorLevel = message.Level.IsMoreSevereThan(_mostSevereErrorLevel) ? message.Level : _mostSevereErrorLevel;
            UpdateHasError();

            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                _logMessages.Add(message);
                while (_logMessages.Count > LogMessageMaxCount)
                {
                    _logMessages.RemoveAt(0);
                }
            });
        }

        private void OnEnableWithDeveloperModeChanged(object? sender, PropertyChangedEventArgs e)
        {
            ShouldRestartToApplyDeveloperMode.Value =
                IsDeveloperMode.Value && _buddyData.IsEnabledWithoutDeveloperMode.Value;
        }

        private void UpdateHasError()
        {
            // 書いてる通りではあるが、開発者モードでは「Fatalではないがスクリプトから自己申告したエラー」もエラーと見なす
            var threshold = IsDeveloperMode.Value
                ? BuddyLogLevel.Error
                : BuddyLogLevel.Fatal;
            HasError.Value = _mostSevereErrorLevel.IsEqualOrMoreSevereThan(threshold);
        }

        private async void ResetSettingsAsync()
        {
            var message = MessageIndication.ResetSingleBuddySettings();
            var result = await MessageBoxWrapper.Instance.ShowAsync(
                message.Title,
                string.Format(message.Content, DisplayName.Value),
                MessageBoxWrapper.MessageBoxStyle.OKCancel
                )
                .ConfigureAwait(true);

            if (!result)
            {
                return;
            }

            // NOTE: プロパティ変更の通知は個別でバシバシ飛んでしまうが許容している
            foreach(var property in Properties)
            {
                property.ResetToDefault();
            }
        }

        private void CopyLogMessage()
        {
            if (IsDeveloperMode.Value)
            {
                if (_logMessages.Count > 0)
                {
                    Clipboard.SetText(string.Join("\n", _logMessages.Select(m => m.Message)));
                    SnackbarWrapper.Enqueue(LocalizedString.GetString("Snackbar_General_TextCopied"));
                }
            }
            else
            {
                if (CurrentFatalError.Value != null)
                {
                    Clipboard.SetText(CurrentFatalError.Value.Message);
                    SnackbarWrapper.Enqueue(LocalizedString.GetString("Snackbar_General_TextCopied"));
                }
            }
        }

        // NOTE: Enqueueとのタイミングを考えてBeginInvokeしてもいいが、まあいい加減に…
        private void ClearLog()
        {
            _logMessages.Clear();
            _mostSevereErrorLevel = BuddyLogLevel.Verbose;
            CurrentFatalError.Value = null;
            HasError.Value = false;
        }

        private void OpenLogFile()
        {
            // TODO: デフォルトサブキャラのログフォルダを分断する場合、ここで分岐させたい
            var filePath = SpecialFilePath.GetBuddyLogFilePath(
                FolderName,
                _buddyData.Metadata.IsDefaultBuddy
                );
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
