namespace Baku.VMagicMirrorConfig.StreamingTabViewModels
{
    //NOTE: 配信タブは機能が雑多なため1タブ = 1ViewModelではなく、中のサブグループ1つに対して1つのViewModelを当てていく
    public class WindowViewModel : ViewModelBase
    {
        public WindowViewModel()
        {
            _model = ModelResolver.Instance.Resolve<WindowSettingModel>();
            BackgroundImageSetCommand = new ActionCommand(_model.SetBackgroundImage);
            BackgroundImageClearCommand = new ActionCommand(
                () => _model.BackgroundImagePath.Value = ""
                );
        }

        private readonly WindowSettingModel _model;

        public RProperty<bool> IsTransparent => _model.IsTransparent;
        public RProperty<bool> WindowDraggable => _model.WindowDraggable;

        public ActionCommand BackgroundImageSetCommand { get; }
        public ActionCommand BackgroundImageClearCommand { get; }
    }

    public class FaceViewModel : ViewModelBase
    {

    }

    public class MotionViewModel : ViewModelBase
    {
    }

    public class VisibilityViewModel : ViewModelBase
    {
        
    }

    public class CameraViewModel : ViewModelBase
    {
    }

    public class DeviceLayoutViewModel : ViewModelBase
    {

    }

    public class WordToMotionViewModel : ViewModelBase
    {
    }
}
