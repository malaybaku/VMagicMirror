using System;
using System.ComponentModel;
using System.Windows;

namespace Baku.VMagicMirrorConfig
{
    //TODO: このクラスに書く処理を「Windowが最初に出たときの処理」と「アプリ終了処理」だけにしたい
    //  →その後に2つの処理をAppクラスに移動してもよい

    public class MainWindowViewModel : ViewModelBase, IWindowViewModel
    {
        internal RootSettingModel Model { get; }
        internal SettingFileIo SettingFileIo { get; }
        internal SaveFileManager SaveFileManager { get; }

        internal MessageIo MessageIo { get; }
        internal IMessageSender MessageSender => MessageIo.Sender;

        public LightSettingViewModel LightSetting { get; private set; }
        public WordToMotionSettingViewModel WordToMotionSetting { get; private set; }
        public ExternalTrackerViewModel ExternalTrackerSetting { get; private set; }
        public SettingIoViewModel SettingIo { get; private set; }

        private readonly AvatarLoader _avatarLoader;
        private readonly AppQuitSetting _appQuitSetting;
        private readonly RuntimeHelper _runtimeHelper;
        private bool _isDisposed = false;

        public MainWindowViewModel()
        {
            Model = ModelResolver.Instance.Resolve<RootSettingModel>();
            SettingFileIo = ModelResolver.Instance.Resolve<SettingFileIo>();
            SaveFileManager = ModelResolver.Instance.Resolve<SaveFileManager>();
            MessageIo = ModelResolver.Instance.Resolve<MessageIo>();
            _appQuitSetting = ModelResolver.Instance.Resolve<AppQuitSetting>();
            _avatarLoader = ModelResolver.Instance.Resolve<AvatarLoader>();

            //TODO: この下のViewModel達は必要に応じて生成されるようにしたい
            LightSetting = new LightSettingViewModel();
            WordToMotionSetting = new WordToMotionSettingViewModel();
            ExternalTrackerSetting = new ExternalTrackerViewModel();
            SettingIo = new SettingIoViewModel();

            //オートメーションの配線: 1つしかないのでザツにやる。OC<T>をいじる関係でUIスレッド必須なことに注意
            Model.Automation.LoadSettingFileRequested += v => 
                Application.Current.Dispatcher.BeginInvoke(new Action(
                    () => SaveFileManager.LoadSetting(v.Index, v.LoadCharacter, v.LoadNonCharacter, true))
                    );

            _runtimeHelper = new RuntimeHelper(MessageSender, MessageIo.Receiver, Model);
        }

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

            //NOTE: よそでやっても良い気がする…
            await ModelResolver.Instance.Resolve<DeviceListSource>().InitializeDeviceNamesAsync();
            await LightSetting.InitializeQualitySelectionsAsync();
            await WordToMotionSetting.InitializeCustomMotionClipNamesAsync();

            _runtimeHelper.Start();

            if (Model.AutoLoadLastLoadedVrm.Value && !string.IsNullOrEmpty(Model.LastVrmLoadFilePath))
            {
                _avatarLoader.LoadLastLoadedLocalVrm();
            }
            else if (Model.AutoLoadLastLoadedVrm.Value && !string.IsNullOrEmpty(Model.LastLoadedVRoidModelId))
            {
                _avatarLoader.LoadSavedVRoidModelAsync(Model.LastLoadedVRoidModelId, true);
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

            if (!_appQuitSetting.SkipAutoSaveAndRestart)
            {
                SettingFileIo.SaveSetting(SpecialFilePath.AutoSaveSettingFilePath, SettingFileReadWriteModes.AutoSave);
            }
            Model.Automation.Dispose();
            MessageIo.Dispose();
            _runtimeHelper.Dispose();
            LargePointerController.Instance.Close();

            //UX的には再起動を意味する
            if (_appQuitSetting.SkipAutoSaveAndRestart)
            {
                UnityAppStarter.StartUnityApp();
            }
        }
    }

    public interface IWindowViewModel : IDisposable
    {
        void Initialize();
    }
}
