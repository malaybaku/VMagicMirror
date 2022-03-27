using Baku.VMagicMirrorConfig.View;
using System;
using System.Windows;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    public class MainWindowViewModel : ViewModelBase, IWindowViewModel
    {
        private readonly RootSettingModel _settingModel;

        private readonly SaveFileManager _saveFileManager;
        private readonly MessageIo _messageIo;        
        private readonly AvatarLoader _avatarLoader;
        private readonly AppQuitSetting _appQuitSetting;
        private readonly RuntimeHelper _runtimeHelper;
        private bool _isDisposed = false;

        public MainWindowViewModel()
        {
            _settingModel = ModelResolver.Instance.Resolve<RootSettingModel>();
            _saveFileManager = ModelResolver.Instance.Resolve<SaveFileManager>();
            _messageIo = ModelResolver.Instance.Resolve<MessageIo>();
            _appQuitSetting = ModelResolver.Instance.Resolve<AppQuitSetting>();
            _avatarLoader = ModelResolver.Instance.Resolve<AvatarLoader>();
            _runtimeHelper = ModelResolver.Instance.Resolve<RuntimeHelper>();
        }

        public async void Initialize()
        {
            if (IsInDesignMode)
            {
                return;
            }

            _messageIo.Start();
            LanguageSelector.Instance.Initialize(_messageIo.Sender);

            _saveFileManager.LoadAutoSave();
            //NOTE: 初回起動時だけカルチャベースで言語を設定する処理
            _settingModel.InitializeLanguageIfNeeded();

            //NOTE: 以下は他のモデルクラスでやるほうが良いのでは…？
            _settingModel.Automation.LoadSettingFileRequested += v =>
                Application.Current.Dispatcher.BeginInvoke(new Action(
                    () => _saveFileManager.LoadSetting(v.Index, v.LoadCharacter, v.LoadNonCharacter, true))
                    );

            await ModelResolver.Instance.Resolve<DeviceListSource>().InitializeDeviceNamesAsync();
            await ModelResolver.Instance.Resolve<ImageQualitySetting>().InitializeQualitySelectionsAsync();
            await ModelResolver.Instance.Resolve<CustomMotionList>().InitializeCustomMotionClipNamesAsync();
            
            _runtimeHelper.Start();

            if (_settingModel.AutoLoadLastLoadedVrm.Value && !string.IsNullOrEmpty(_settingModel.LastVrmLoadFilePath))
            {
                _avatarLoader.LoadLastLoadedLocalVrm();
            }
            else if (_settingModel.AutoLoadLastLoadedVrm.Value && !string.IsNullOrEmpty(_settingModel.LastLoadedVRoidModelId))
            {
                _avatarLoader.LoadSavedVRoidModelAsync(_settingModel.LastLoadedVRoidModelId, true);
            }

            //NOTE: このへんはとりわけ起動直後に1回だけ呼びたい処理であることに注意
            ModelResolver.Instance.Resolve<ExternalTrackerSettingModel>().RefreshConnectionIfPossible();
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
                _saveFileManager.SaveAsAutoSave();
            }
            _settingModel.Automation.Dispose();
            _messageIo.Dispose();
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
