using Baku.VMagicMirrorConfig.View;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    /// <summary>
    /// ホームタブ部分のビューモデル
    /// </summary>
    public class HomeViewModel : ViewModelBase
    {
        private readonly RootSettingModel _setting;
        private readonly SaveFileManager _saveFileManager;
        private readonly AvatarLoader _avatarLoader;
        private readonly AppQuitSetting _appQuitSetting;

        public HomeViewModel() : this(
            ModelResolver.Instance.Resolve<RootSettingModel>(),
            ModelResolver.Instance.Resolve<SaveFileManager>(),
            ModelResolver.Instance.Resolve<AvatarLoader>(),
            ModelResolver.Instance.Resolve<ScreenshotTaker>(),
            ModelResolver.Instance.Resolve<AppQuitSetting>()
            )
        {
        }

        internal HomeViewModel(
            RootSettingModel rootSetting, 
            SaveFileManager saveFileManager,
            AvatarLoader avatarLoader,
            ScreenshotTaker screenshotTaker,
            AppQuitSetting appQuitSetting
            )
        {
            _setting = rootSetting;
            _saveFileManager = saveFileManager;
            _avatarLoader = avatarLoader;
            _appQuitSetting = appQuitSetting;
            
            LoadVrmCommand = new ActionCommand(LoadVrmByFileOpenDialog);
            LoadVrmByFilePathCommand = new ActionCommand<string>(LoadVrmByFilePath);
            ConnectToVRoidHubCommand = new ActionCommand(async () => await _avatarLoader.ConnectToVRoidHubAsync());
            OpenVRoidHubCommand = new ActionCommand(() => UrlNavigate.Open("https://hub.vroid.com/"));

            AutoAdjustCommand = new ActionCommand(() => avatarLoader.RequestAutoAdjust());
            OpenSettingWindowCommand = new ActionCommand(() => SettingWindow.OpenOrActivateExistingWindow());

            TakeScreenshotCommand = new ActionCommand(() => screenshotTaker.TakeScreenshot());
            OpenScreenshotFolderCommand = new ActionCommand(() => screenshotTaker.OpenScreenshotSavedFolder());

            ShowSaveModalCommand = new ActionCommand(ShowSaveModal);
            ShowLoadModalCommand = new ActionCommand(ShowLoadModal);

            ExportSettingToFileCommand = new ActionCommand(SaveSettingToFile);
            ImportSettingFromFileCommand = new ActionCommand(LoadSettingFromFile);

            ResetToDefaultCommand = new ActionCommand(ResetToDefault);

            if (IsInDesignMode)
            {
                return;
            }

            //NOTE: RegSettingのモデル的な層をもっとキレイにしてもOK
            var regSetting = new StartupRegistrySetting();
            _activateOnStartup = regSetting.CheckThisVersionRegistered();
            if (_activateOnStartup)
            {
                RaisePropertyChanged(nameof(ActivateOnStartup));
                LogOutput.Instance.Write($"prop changed activate on startup");
            }
            OtherVersionRegisteredOnStartup = regSetting.CheckOtherVersionRegistered();
        }


        public RProperty<bool> AutoLoadLastLoadedVrm => _setting.AutoLoadLastLoadedVrm;

        public ReadOnlyObservableCollection<string> AvailableLanguageNames => LanguageSelector.Instance.AvailableLanguageNames;
        public RProperty<string> LanguageName => _setting.LanguageName;
        public RProperty<bool> MinimizeOnLaunch => _setting.MinimizeOnLaunch;

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
                    LogOutput.Instance.Write($"Register this version to startup, {value}");
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
            _saveFileManager.SettingFileIo.DeleteSetting(SpecialFilePath.AutoSaveSettingFilePath);
            //オートセーブじゃないほうの設定ファイルも消してしまう
            ModelResolver.Instance.Resolve<PreferenceFileManager>().DeleteSaveFile();
            Application.Current.MainWindow?.Close();
        }

        #region セーブ/ロード

        public ActionCommand ShowSaveModalCommand { get; }
        public ActionCommand ShowLoadModalCommand { get; }

        private async void ShowSaveModal()
        {
            if (Application.Current.MainWindow is not MetroWindow window)
            {
                return;
            }

            var progress = await GuardSettingWindowIfNeeded();

            var dialog = new SaveLoadMetroDialog();
            var vm = SaveLoadDataViewModel.CreateForSave(_saveFileManager, async () =>
            {
                await window.HideMetroDialogAsync(dialog);
                if (progress != null)
                {
                    await progress.CloseAsync();
                }
            });

            dialog.DataContext = vm;
            await window.ShowMetroDialogAsync(dialog, new MetroDialogSettings()
            {
                AnimateShow = true,
                AnimateHide = false,
                OwnerCanCloseWithDialog = true,
            });
            await dialog.WaitUntilUnloadedAsync();
        }

        private async void ShowLoadModal()
        {
            if (Application.Current.MainWindow is not MetroWindow window)
            {
                return;
            }

            var progress = await GuardSettingWindowIfNeeded();

            var dialog = new SaveLoadMetroDialog();
            var vm = SaveLoadDataViewModel.CreateForLoad(_setting, _saveFileManager, async () =>
            {
                await window.HideMetroDialogAsync(dialog);
                if (progress != null)
                {
                    await progress.CloseAsync();
                }
            });

            dialog.DataContext = vm;
            await window.ShowMetroDialogAsync(dialog, new MetroDialogSettings()
            {
                AnimateShow = true,
                AnimateHide = false,
                OwnerCanCloseWithDialog = true,
            });
            await dialog.WaitUntilUnloadedAsync();
        }

        private async Task<ProgressDialogController?> GuardSettingWindowIfNeeded()
        {
            if (SettingWindow.CurrentWindow is not SettingWindow settingWindow)
            {
                return null;
            }

            var indication = MessageIndication.GuardSettingWindowDuringSaveLoad();
            return await settingWindow.ShowProgressAsync(indication.Title, indication.Content, settings: new MetroDialogSettings()
            {
                DialogResultOnCancel = MessageDialogResult.Negative,
                AnimateShow = true,
                AnimateHide = false,
                OwnerCanCloseWithDialog = true,
            });
        }

        #endregion

        #region エクスポート/インポート

        public ActionCommand ExportSettingToFileCommand { get; }
        public ActionCommand ImportSettingFromFileCommand { get; }

        private void SaveSettingToFile()
        {
            var dialog = new SaveFileDialog()
            {
                Title = "Save VMagicMirror Setting",
                Filter = "VMagicMirror Setting File(*.vmm)|*.vmm",
                DefaultExt = ".vmm",
                AddExtension = true,
            };
            if (dialog.ShowDialog() == true)
            {
                _saveFileManager.SettingFileIo.SaveSetting(dialog.FileName, SettingFileReadWriteModes.Exported);
                SnackbarWrapper.Enqueue(LocalizedString.GetString("SettingFile_SaveCompleted_ExportedFile"));
            }
        }

        private void LoadSettingFromFile()
        {
            var dialog = new OpenFileDialog()
            {
                Title = "Load VMagicMirror Setting",
                Filter = "VMagicMirror Setting File (*.vmm)|*.vmm",
                Multiselect = false,
            };
            if (dialog.ShowDialog() == true)
            {
                _saveFileManager.SettingFileIo.LoadSetting(dialog.FileName, SettingFileReadWriteModes.Exported);
                SnackbarWrapper.Enqueue(LocalizedString.GetString("SettingFile_LoadCompleted_ExportedFile"));
            }
        }

        #endregion
    }
}
