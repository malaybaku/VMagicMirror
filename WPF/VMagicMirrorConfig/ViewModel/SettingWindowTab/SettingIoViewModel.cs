using Baku.VMagicMirrorConfig.View;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System.Threading.Tasks;
using System.Windows;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    public class SettingIoViewModel : SettingViewModelBase
    {
        public SettingIoViewModel() : this(
            ModelResolver.Instance.Resolve<RootSettingModel>(),
            ModelResolver.Instance.Resolve<AutomationSettingModel>(),
            ModelResolver.Instance.Resolve<SaveFileManager>()
            )
        {
        }

        internal SettingIoViewModel(
            RootSettingModel rootModel, AutomationSettingModel model, SaveFileManager saveFileManager
            )
        {
            _rootModel = rootModel;
            _model = model;
            _saveFileManager = saveFileManager;

            OpenInstructionUrlCommand = new ActionCommand(OpenInstructionUrl);
            RequestEnableAutomationCommand = new ActionCommand(OnEnableAutomationRequested);
            RequestDisableAutomationCommand = new ActionCommand(OnDisableAutomationRequested);
            ApplyPortNumberCommand = new ActionCommand(ApplyPortNumber);

            ShowSaveModalCommand = new ActionCommand(ShowSaveModal);
            ShowLoadModalCommand = new ActionCommand(ShowLoadModal);

            ExportSettingToFileCommand = new ActionCommand(SaveSettingToFile);
            ImportSettingFromFileCommand = new ActionCommand(LoadSettingFromFile);

            if (!IsInDegignMode)
            {
                AutomationPortNumberText = new RProperty<string>("");
                return;
            }

            AutomationPortNumberText = new RProperty<string>(
                _model.AutomationPortNumber.Value.ToString(), v =>
                {
                    //フォーマット違反になってないかチェック
                    PortNumberIsInvalid.Value = !(int.TryParse(v, out int i) && i >= 0 && i < 65536);
                });

            _model.AutomationPortNumber.PropertyChanged += (_, __) =>
            {
                AutomationPortNumberText.Value = _model.AutomationPortNumber.Value.ToString();
            };
        }

        //NOTE: rootが必要なのは「ロード時にキャラ情報/非キャラ情報をどう扱うか」という値がRootに入っているため。
        //コレ以外の目的で使うのは濫用に当たるので注意
        private readonly RootSettingModel _rootModel;
        private readonly AutomationSettingModel _model;
        private readonly SaveFileManager _saveFileManager;

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
            var vm = SaveLoadDataViewModel.CreateForLoad(_rootModel, _saveFileManager, async () =>
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

        #region オートメーションっぽい所

        public RProperty<bool> IsAutomationEnabled => _model.IsAutomationEnabled;

        public RProperty<string> AutomationPortNumberText { get; }
        //NOTE: Converter使うのも違う気がするのでViewModel層でやってしまう
        public RProperty<bool> PortNumberIsInvalid { get; } = new RProperty<bool>(false);

        public ActionCommand OpenInstructionUrlCommand { get; }
        public ActionCommand RequestEnableAutomationCommand { get; }
        public ActionCommand RequestDisableAutomationCommand { get; }
        public ActionCommand ApplyPortNumberCommand { get; }

        private async void OnEnableAutomationRequested()
        {
            var indication = MessageIndication.EnableAutomation();
            var result = await MessageBoxWrapper.Instance.ShowAsync(
                indication.Title, indication.Content, MessageBoxWrapper.MessageBoxStyle.OKCancel
                );

            if (result)
            {
                _model.IsAutomationEnabled.Value = true;
            }
        }

        private async void OnDisableAutomationRequested()
        {
            var indication = MessageIndication.DisableAutomation();
            var result = await MessageBoxWrapper.Instance.ShowAsync(
                indication.Title, indication.Content, MessageBoxWrapper.MessageBoxStyle.OKCancel
                );

            if (result)
            {
                _model.IsAutomationEnabled.Value = false;
            }
        }

        private void ApplyPortNumber()
        {
            if (int.TryParse(AutomationPortNumberText.Value, out int i) && i >= 0 && i < 65536)
            {
                _model.AutomationPortNumber.Value = i;
            }
        }

        //NOTE: オートメーションの説明ではあるけど設定ファイルタブ全体の設定に飛ばす。
        //どのみちファイルI/Oがどうなってるか説明する必要あるので
        private void OpenInstructionUrl()
            => UrlNavigate.Open(LocalizedString.GetString("URL_docs_setting_files"));

        #endregion

    }
}
