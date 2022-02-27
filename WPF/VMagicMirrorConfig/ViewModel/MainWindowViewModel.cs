using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace Baku.VMagicMirrorConfig
{
    public class MainWindowViewModel : ViewModelBase, IWindowViewModel
    {
        internal RootSettingSync Model { get; }
        internal SettingFileIo SettingFileIo { get; }
        internal SaveFileManager SaveFileManager { get; }

        internal MessageIo MessageIo { get; } = new MessageIo();
        internal IMessageSender MessageSender => MessageIo.Sender;

        public WindowSettingViewModel WindowSetting { get; private set; }
        public MotionSettingViewModel MotionSetting { get; private set; }
        public LayoutSettingViewModel LayoutSetting { get; private set; }
        public GamepadSettingViewModel GamepadSetting { get; private set; }
        public LightSettingViewModel LightSetting { get; private set; }
        public WordToMotionSettingViewModel WordToMotionSetting { get; private set; }
        public ExternalTrackerViewModel ExternalTrackerSetting { get; private set; }
        public AccessorySettingViewModel AccessorySetting { get; private set; }
        public SettingIoViewModel SettingIo { get; private set; }

        private readonly RuntimeHelper _runtimeHelper;
        private bool _isDisposed = false;
        //VRoid Hubに接続中かどうか
        private bool _isVRoidHubUiActive = false;

        //NOTE: モデルのロード確認UI(ファイル/VRoidHubいずれか)を出す直前時点での値を保持するフラグで、UIが出てないときはnullになる
        private bool? _windowTransparentBeforeLoadProcess = null;

        private bool _settingResetCalled = false;

        public MainWindowViewModel()
        {
            Model = new RootSettingSync(MessageSender, MessageIo.Receiver);
            SettingFileIo = new SettingFileIo(Model, MessageSender);
            SaveFileManager = new SaveFileManager(SettingFileIo, Model, MessageSender);

            WindowSetting = new WindowSettingViewModel(Model.Window, MessageSender);
            MotionSetting = new MotionSettingViewModel(Model.Motion, MessageSender, MessageIo.Receiver);
            GamepadSetting = new GamepadSettingViewModel(Model.Gamepad, MessageSender);
            LayoutSetting = new LayoutSettingViewModel(Model.Layout, Model.Gamepad, MessageSender, MessageIo.Receiver);
            LightSetting = new LightSettingViewModel(Model.Light, MessageSender);
            WordToMotionSetting = new WordToMotionSettingViewModel(
                Model.WordToMotion, Model.Layout, Model.Accessory, MessageSender, MessageIo.Receiver);
            ExternalTrackerSetting = new ExternalTrackerViewModel(
                Model.ExternalTracker, Model.Motion, Model.Accessory, MessageSender, MessageIo.Receiver);
            AccessorySetting = new AccessorySettingViewModel(Model.Accessory, Model.Layout);
            SettingIo = new SettingIoViewModel(Model, Model.Automation, SaveFileManager, MessageSender);
            //オートメーションの配線: 1つしかないのでザツにやる。OC<T>をいじる関係でUIスレッド必須なことに注意
            Model.Automation.LoadSettingFileRequested += v => 
                Application.Current.Dispatcher.BeginInvoke(new Action(
                    () => SaveFileManager.LoadSetting(v.Index, v.LoadCharacter, v.LoadNonCharacter, true))
                    );

            _runtimeHelper = new RuntimeHelper(MessageSender, MessageIo.Receiver, Model);

            LoadVrmCommand = new ActionCommand(LoadVrm);
            LoadVrmByFilePathCommand = new ActionCommand<string>(LoadVrmByFilePath);
            ConnectToVRoidHubCommand = new ActionCommand(ConnectToVRoidHubAsync);

            OpenVRoidHubCommand = new ActionCommand(() => UrlNavigate.Open("https://hub.vroid.com/"));
            AutoAdjustCommand = new ActionCommand(() => MessageSender.SendMessage(MessageFactory.Instance.RequestAutoAdjust()));
            OpenSettingWindowCommand = new ActionCommand(() => SettingWindow.OpenOrActivateExistingWindow(this));

            ResetToDefaultCommand = new ActionCommand(ResetToDefault);
            LoadPrevSettingCommand = new ActionCommand(LoadPrevSetting);

            TakeScreenshotCommand = new ActionCommand(_runtimeHelper.TakeScreenshot);
            OpenScreenshotFolderCommand = new ActionCommand(_runtimeHelper.OpenScreenshotSavedFolder);

            MessageIo.Receiver.ReceivedCommand += OnReceiveCommand;
            SaveFileManager.VRoidModelLoadRequested += id => LoadSavedVRoidModel(id, false);
        }

        private void OnReceiveCommand(object? sender, CommandReceivedEventArgs e)
        {
            switch (e.Command)
            {
                case ReceiveMessageNames.VRoidModelLoadCompleted:
                    //WPF側のダイアログによるUIガードを終了: _isVRoidHubUiActiveフラグは別のとこで折るのでここでは無視でOK
                    if (_isVRoidHubUiActive)
                    {
                        MessageBoxWrapper.Instance.SetDialogResult(false);
                    }

                    //ファイルパスではなくモデルID側を最新情報として覚えておく
                    Model.OnVRoidModelLoaded(e.Args);

                    break;
                case ReceiveMessageNames.VRoidModelLoadCanceled:
                    //WPF側のダイアログによるUIガードを終了
                    if (_isVRoidHubUiActive)
                    {
                        MessageBoxWrapper.Instance.SetDialogResult(false);
                    }
                    break;
                case ReceiveMessageNames.ModelNameConfirmedOnLoad:
                    //ともかくモデルがロードされているため、実態に合わせておく
                    Model.LoadedModelName = e.Args;
                    break;
            }
        }

        #region Properties

        public RProperty<bool> AutoLoadLastLoadedVrm => Model.AutoLoadLastLoadedVrm;

        public ReadOnlyObservableCollection<string> AvailableLanguageNames => Model.AvailableLanguageNames;
        public RProperty<string> LanguageName => Model.LanguageName;

        private bool _activateOnStartup = false;
        public bool ActivateOnStartup
        {
            get => _activateOnStartup;
            set
            {
                if (SetValue(ref _activateOnStartup, value))
                {
                    new StartupRegistrySetting().SetThisVersionRegister(value);
                    if (value)
                    {
                        OtherVersionRegisteredOnStartup = false;
                    }
                }
            }
        }

        private bool _otherVersionRegisteredOnStartup = false;
        public bool OtherVersionRegisteredOnStartup
        {
            get => _otherVersionRegisteredOnStartup;
            private set => SetValue(ref _otherVersionRegisteredOnStartup, value);
        }

        #endregion

        #region Commands

        public ActionCommand LoadVrmCommand { get; }
        public ActionCommand<string> LoadVrmByFilePathCommand { get; }
        public ActionCommand ConnectToVRoidHubCommand { get; }

        public ActionCommand OpenVRoidHubCommand { get; }
        public ActionCommand AutoAdjustCommand { get; }
        public ActionCommand OpenSettingWindowCommand { get; }

        public ActionCommand ResetToDefaultCommand { get; }
        
        public ActionCommand LoadPrevSettingCommand { get; }

        public ActionCommand TakeScreenshotCommand { get; }
        public ActionCommand OpenScreenshotFolderCommand { get; }

        #endregion

        #region Command Impl

        //NOTE: async voidを使ってるが、ここはUIイベントのハンドラ相当なので許して

        private async void LoadVrm()
        {
            await LoadVrmSub(() =>
            {
                var dialog = new OpenFileDialog()
                {
                    Title = "Open VRM file",
                    Filter = "VRM files (*.vrm)|*.vrm|All files (*.*)|*.*",
                    Multiselect = false,
                };

                return
                    (dialog.ShowDialog() == true && File.Exists(dialog.FileName))
                    ? dialog.FileName
                    : "";
            });
        }

        private async void LoadVrmByFilePath(string? filePath)
        {
            if (filePath != null && File.Exists(filePath) && Path.GetExtension(filePath) == ".vrm")
            {
                await LoadVrmSub(() => filePath);
            }
        }

        /// <summary>
        /// ファイルパスを取得する処理を指定して、VRMをロードします。
        /// </summary>
        /// <param name="getFilePathProcess"></param>
        private async Task LoadVrmSub(Func<string> getFilePathProcess)
        {
            PrepareShowUiOnUnity();

            string filePath = getFilePathProcess();
            if (!File.Exists(filePath))
            {
                EndShowUiOnUnity();
                return;
            }

            MessageSender.SendMessage(MessageFactory.Instance.OpenVrmPreview(filePath));

            var indication = MessageIndication.LoadVrmConfirmation();
            bool res = await MessageBoxWrapper.Instance.ShowAsync(
                indication.Title,
                indication.Content,
                MessageBoxWrapper.MessageBoxStyle.OKCancel
                );

            if (res)
            {
                MessageSender.SendMessage(MessageFactory.Instance.OpenVrm(filePath));
                Model.OnLocalModelLoaded(filePath);
            }
            else
            {
                MessageSender.SendMessage(MessageFactory.Instance.CancelLoadVrm());
            }

            EndShowUiOnUnity();
        }

        private async void ConnectToVRoidHubAsync()
        {
            PrepareShowUiOnUnity();

            MessageSender.SendMessage(MessageFactory.Instance.OpenVRoidSdkUi());

            //VRoidHub側の操作が終わるまでダイアログでガードをかける: モーダル的な管理状態をファイルロードの場合と揃える為
            _isVRoidHubUiActive = true;
            var message = MessageIndication.ShowVRoidSdkUi();
            bool _ = await MessageBoxWrapper.Instance.ShowAsync(
                message.Title, message.Content, MessageBoxWrapper.MessageBoxStyle.None
                );

            //モデルロード完了またはキャンセルによってここに来るので、共通の処理をして終わり
            _isVRoidHubUiActive = false;
            EndShowUiOnUnity();
        }

        private async void ResetToDefault()
        {
            var indication = MessageIndication.ResetSettingConfirmation();
            bool res = await MessageBoxWrapper.Instance.ShowAsync(
                indication.Title,
                indication.Content,
                MessageBoxWrapper.MessageBoxStyle.OKCancel
                );

            if (!res)
            {
                return;
            }

            //設定を消したあとで再起動
            _settingResetCalled = true;
            SaveFileManager.SettingFileIo.DeleteSetting(SpecialFilePath.AutoSaveSettingFilePath);
            Application.Current.MainWindow?.Close();
        }

        private void LoadPrevSetting()
        {
            var dialog = new OpenFileDialog()
            {
                Title = "Select Previous Version VMagicMirror.exe",
                Filter = "VMagicMirror.exe|VMagicMirror.exe",
                Multiselect = false,
            };
            if (dialog.ShowDialog() != true || string.IsNullOrEmpty(dialog.FileName))
            {
                return;
            }

            try
            {
                //TODO: この位置にファイルがあるのはzipで配布された古いバージョンのみになる
                string prevFilePath = Path.Combine(
                    Path.GetDirectoryName(dialog.FileName) ?? "",
                    "ConfigApp",
                    SpecialFilePath.AutoSaveSettingFileName
                    );

                SettingFileIo.LoadSetting(prevFilePath, SettingFileReadWriteModes.AutoSave);
                //NOTE: VRoidの自動ロード設定はちょっと概念的に重たいので引き継ぎ対象から除外
                Model.LastLoadedVRoidModelId = "";
                if (Model.AutoLoadLastLoadedVrm.Value && !string.IsNullOrEmpty(Model.LastVrmLoadFilePath))
                {
                    LoadLastLoadedLocalVrm();
                }
            }
            catch (Exception ex)
            {
                var indication = MessageIndication.ErrorLoadSetting();
                MessageBox.Show(
                    indication.Title,
                    indication.Content + ex.Message
                    );
            }
        }

        #endregion

        public async void Initialize()
        {
            if (Application.Current.MainWindow == null ||
                DesignerProperties.GetIsInDesignMode(Application.Current.MainWindow))
            {
                return;
            }

            MessageIo.Start();
            LanguageSelector.Instance.Initialize(MessageSender);
            Model.InitializeAvailableLanguage(
                LanguageSelector.Instance.GetAdditionalSupportedLanguageNames()
                );

            SettingFileIo.LoadSetting(SpecialFilePath.AutoSaveSettingFilePath, SettingFileReadWriteModes.AutoSave);

            //NOTE: 初回起動時だけカルチャベースで言語を設定するための処理がコレ
            Model.InitializeLanguageIfNeeded();

            await MotionSetting.InitializeDeviceNamesAsync();
            await LightSetting.InitializeQualitySelectionsAsync();
            await WordToMotionSetting.InitializeCustomMotionClipNamesAsync();

            var regSetting = new StartupRegistrySetting();
            _activateOnStartup = regSetting.CheckThisVersionRegistered();
            if (_activateOnStartup)
            {
                RaisePropertyChanged(nameof(ActivateOnStartup));
            }
            OtherVersionRegisteredOnStartup = regSetting.CheckOtherVersionRegistered();

            _runtimeHelper.Start();

            if (AutoLoadLastLoadedVrm.Value && !string.IsNullOrEmpty(Model.LastVrmLoadFilePath))
            {
                LoadLastLoadedLocalVrm();
            }
            else if (AutoLoadLastLoadedVrm.Value && !string.IsNullOrEmpty(Model.LastLoadedVRoidModelId))
            {
                LoadSavedVRoidModel(Model.LastLoadedVRoidModelId, true);
            }

            //NOTE: このへんの処理は起動直後限定の処理にしたくて意図的にやってます
            ExternalTrackerSetting.RefreshConnectionIfPossible();
            await new UpdateChecker().RunAsync(true);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            if (!_settingResetCalled)
            {
                SettingFileIo.SaveSetting(SpecialFilePath.AutoSaveSettingFilePath, SettingFileReadWriteModes.AutoSave);
            }
            Model.Automation.Dispose();
            MessageIo.Dispose();
            _runtimeHelper.Dispose();
            LargePointerController.Instance.Close();

            //呼ぶのは起動処理だがUX的には再起動
            if (_settingResetCalled)
            {
                UnityAppStarter.StartUnityApp();
            }
        }

        private void LoadLastLoadedLocalVrm()
        {
            if (File.Exists(Model.LastVrmLoadFilePath))
            {
                MessageSender.SendMessage(MessageFactory.Instance.OpenVrm(Model.LastVrmLoadFilePath));
            }
        }

        private async void LoadSavedVRoidModel(string modelId, bool fromAutoSave)
        {
            if (string.IsNullOrEmpty(modelId))
            {
                return;
            }

            PrepareShowUiOnUnity();

            //NOTE: モデルIDを載せる以外は通常のUIオープンと同じフロー
            MessageSender.SendMessage(MessageFactory.Instance.RequestLoadVRoidWithId(modelId));

            _isVRoidHubUiActive = true;
            //自動セーブなら「前回のモデル」だしそれ以外なら「設定ファイルに乗ってたモデル」となる。分けといたほうがわかりやすいので分ける。
            var message = fromAutoSave 
                ? MessageIndication.ShowLoadingPreviousVRoid() 
                : MessageIndication.ShowLoadingSavedVRoidModel();
            bool _ = await MessageBoxWrapper.Instance.ShowAsync(
                message.Title, message.Content, MessageBoxWrapper.MessageBoxStyle.None
                );

            //モデルロード完了またはキャンセルによってここに来るので、共通の処理をして終わり
            _isVRoidHubUiActive = false;
            EndShowUiOnUnity();
        }

        //Unity側でウィンドウを表示するとき、最前面と透過を無効にする必要があるため、その準備にあたる処理を行います。
        private void PrepareShowUiOnUnity()
        {
            _windowTransparentBeforeLoadProcess = WindowSetting.IsTransparent.Value;
            WindowSetting.IsTransparent.Value = false;
        }

        //Unity側でのUI表示が終わったとき、最前面と透過の設定をもとの状態に戻します。
        private void EndShowUiOnUnity()
        {
            if (_windowTransparentBeforeLoadProcess != null)
            {
                WindowSetting.IsTransparent.Value = _windowTransparentBeforeLoadProcess.GetValueOrDefault();
                _windowTransparentBeforeLoadProcess = null;
            }
        }
    }

    public interface IWindowViewModel : IDisposable
    {
        void Initialize();
    }
}
