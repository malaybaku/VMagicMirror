using Baku.VMagicMirrorConfig.View;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    /// <summary>
    /// ホームタブ部分のビューモデル
    /// </summary>
    public class HomeViewModel : ViewModelBase
    {
        private readonly RootSettingModel _setting;
        private readonly SettingFileIo _settingFileIo;
        private readonly AvatarLoader _avatarLoader;
        private readonly AppQuitSetting _appQuitSetting;

        public HomeViewModel() : this(
            ModelResolver.Instance.Resolve<RootSettingModel>(),
            ModelResolver.Instance.Resolve<SettingFileIo>(),
            ModelResolver.Instance.Resolve<AvatarLoader>(),
            ModelResolver.Instance.Resolve<ScreenshotTaker>(),
            ModelResolver.Instance.Resolve<AppQuitSetting>()
            )
        {
        }

        internal HomeViewModel(
            RootSettingModel rootSetting, 
            SettingFileIo settingFileIo,
            AvatarLoader avatarLoader,
            ScreenshotTaker screenshotTaker,
            AppQuitSetting appQuitSetting
            )
        {
            _setting = rootSetting;
            _settingFileIo = settingFileIo;
            _avatarLoader = avatarLoader;
            _appQuitSetting = appQuitSetting;
            
            LoadVrmCommand = new ActionCommand(LoadVrmByFileOpenDialog);
            LoadVrmByFilePathCommand = new ActionCommand<string>(LoadVrmByFilePath);
            ConnectToVRoidHubCommand = new ActionCommand(async () => await _avatarLoader.ConnectToVRoidHubAsync());
            OpenVRoidHubCommand = new ActionCommand(() => UrlNavigate.Open("https://hub.vroid.com/"));

            AutoAdjustCommand = new ActionCommand(() => avatarLoader.RequestAutoAdjust());
            OpenSettingWindowCommand = new ActionCommand(() => SettingWindow.OpenOrActivateExistingWindow());

            ResetToDefaultCommand = new ActionCommand(ResetToDefault);

            TakeScreenshotCommand = new ActionCommand(() => screenshotTaker.TakeScreenshot());
            OpenScreenshotFolderCommand = new ActionCommand(() => screenshotTaker.OpenScreenshotSavedFolder());

            if (!IsInDesignMode)
            {
                return;
            }

            //NOTE: RegSettingのモデル的な層をもっとキレイにしてもOK
            var regSetting = new StartupRegistrySetting();
            _activateOnStartup = regSetting.CheckThisVersionRegistered();
            if (_activateOnStartup)
            {
                RaisePropertyChanged(nameof(ActivateOnStartup));
            }
            OtherVersionRegisteredOnStartup = regSetting.CheckOtherVersionRegistered();
        }


        public RProperty<bool> AutoLoadLastLoadedVrm => _setting.AutoLoadLastLoadedVrm;

        public ReadOnlyObservableCollection<string> AvailableLanguageNames => LanguageSelector.Instance.AvailableLanguageNames;
        public RProperty<string> LanguageName => _setting.LanguageName;

        //NOTE: ここは若干横着だが、モデル側に寄せるほどでも無い気がするのでコレで。
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

        public ActionCommand LoadVrmCommand { get; }
        public ActionCommand<string> LoadVrmByFilePathCommand { get; }
        public ActionCommand ConnectToVRoidHubCommand { get; }

        public ActionCommand OpenVRoidHubCommand { get; }
        public ActionCommand AutoAdjustCommand { get; }
        public ActionCommand OpenSettingWindowCommand { get; }
        public ActionCommand ResetToDefaultCommand { get; }

        public ActionCommand TakeScreenshotCommand { get; }
        public ActionCommand OpenScreenshotFolderCommand { get; }

        private async void LoadVrmByFileOpenDialog()
        {
            await _avatarLoader.LoadVrm(() =>
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
                await _avatarLoader.LoadVrm(() => filePath);
            }
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

            //書いてる通りだが、「設定のデフォルト化 == 設定ファイルを消してオートセーブが無効な状態で再起動」とすることで
            //クリーンに再起動しようとしている
            _appQuitSetting.SkipAutoSaveAndRestart = true;
            _settingFileIo.DeleteSetting(SpecialFilePath.AutoSaveSettingFilePath);
            Application.Current.MainWindow?.Close();
        }
    }
}
