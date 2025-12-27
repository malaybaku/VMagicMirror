using System.ComponentModel;
using System.Windows.Media;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    public class WindowSettingViewModel : SettingViewModelBase
    {
        public WindowSettingViewModel() : this(ModelResolver.Instance.Resolve<WindowSettingModel>())
        {
        }

        internal WindowSettingViewModel(WindowSettingModel model)
        {
            _model = model;

            BackgroundImageSetCommand = new ActionCommand(() => _model.SetBackgroundImage());
            BackgroundImageClearCommand = new ActionCommand(
                () => _model.BackgroundImagePath.Value = ""
                );

            ResetBackgroundColorSettingCommand = new ActionCommand(
                () => SettingResetUtils.ResetSingleCategoryAsync(_model.ResetBackgroundColor)
                );
            ResetWindowPositionCommand = new ActionCommand(() => _model.ResetWindowPosition());
            ResetOpacitySettingCommand = new ActionCommand(
                () => SettingResetUtils.ResetSingleCategoryAsync(_model.ResetOpacity)
                );

            ResetSpoutOutputSettingCommand = new ActionCommand(
                () => SettingResetUtils.ResetSingleCategoryAsync(_model.ResetSpoutOutput)
                );
            ResetCropSettingCommand = new ActionCommand(
                () => SettingResetUtils.ResetSingleCategoryAsync(_model.ResetCrop)
                );

            if (IsInDesignMode)
            {
                return;
            }

            _model.R.AddWeakEventHandler(OnPickerColorChanged);
            _model.G.AddWeakEventHandler(OnPickerColorChanged);
            _model.B.AddWeakEventHandler(OnPickerColorChanged);
            //初期値を反映しないと変な事になるので注意。Cropのほうも同様
            RaisePropertyChanged(nameof(PickerColor));

            _model.CropBorderR.AddWeakEventHandler(OnCropBorderColorChanged);
            _model.CropBorderG.AddWeakEventHandler(OnCropBorderColorChanged);
            _model.CropBorderB.AddWeakEventHandler(OnCropBorderColorChanged);
            RaisePropertyChanged(nameof(CropBorderColor));
        }

        private readonly WindowSettingModel _model;

        public RProperty<int> R => _model.R;
        public RProperty<int> G => _model.G;
        public RProperty<int> B => _model.B;

        private void OnPickerColorChanged(object? sender, PropertyChangedEventArgs e)
        {
            RaisePropertyChanged(nameof(PickerColor));
        }

        private void OnCropBorderColorChanged(object? sender, PropertyChangedEventArgs e)
        {
            RaisePropertyChanged(nameof(PickerColor));
        }

        /// <summary> ColorPickerに表示する、Alphaを考慮しない背景色を取得、設定します。 </summary>
        public Color PickerColor
        {
            get => Color.FromRgb((byte)R.Value, (byte)G.Value, (byte)B.Value);
            set
            {
                R.Value = value.R;
                G.Value = value.G;
                B.Value = value.B;
            }
        }

        public Color CropBorderColor
        {
            get => Color.FromRgb((byte)R.Value, (byte)G.Value, (byte)B.Value);
            set
            {
                CropBorderColorR.Value = value.R;
                CropBorderColorG.Value = value.G;
                CropBorderColorB.Value = value.B;
            }
        }

        public RProperty<bool> IsTransparent => _model.IsTransparent;
        public RProperty<bool> WindowDraggable => _model.WindowDraggable;
        public RProperty<bool> TopMost => _model.TopMost;

        public RProperty<int> WholeWindowTransparencyLevel => _model.WholeWindowTransparencyLevel;
        public RProperty<int> AlphaValueOnTransparent => _model.AlphaValueOnTransparent;

        public RProperty<bool> EnableSpoutOutput => _model.EnableSpoutOutput;
        public RProperty<int> SpoutResolutionType => _model.SpoutResolutionType;

        public RProperty<bool> EnableCircleCrop => _model.EnableCircleCrop;
        public RProperty<float> CircleCropSize => _model.CircleCropSize;
        public RProperty<float> CircleCropBorderWidth => _model.CircleCropBorderWidth;
        public RProperty<int> CropBorderColorR => _model.CropBorderR;
        public RProperty<int> CropBorderColorG => _model.CropBorderG;
        public RProperty<int> CropBorderColorB => _model.CropBorderB;

        public ActionCommand BackgroundImageSetCommand { get; }
        public ActionCommand BackgroundImageClearCommand { get; }

        public ActionCommand ResetWindowPositionCommand { get; }
        public ActionCommand ResetBackgroundColorSettingCommand { get; }
        public ActionCommand ResetOpacitySettingCommand { get; }
        public ActionCommand ResetSpoutOutputSettingCommand { get; }
        public ActionCommand ResetCropSettingCommand { get; }

        public SpoutResolutionTypeNameViewModel[] SpoutResolutionTypes => SpoutResolutionTypeNameViewModel.AvailableItems;
    }

    public class SpoutResolutionTypeNameViewModel
    {

        public SpoutResolutionTypeNameViewModel(SpoutResolutionType type, string localizationKey)
        {
            Type = type;
            _localizationKey = localizationKey;
            Label.Value = LocalizedString.GetString(_localizationKey);
            LanguageSelector.Instance.LanguageChanged +=
                () => Label.Value = LocalizedString.GetString(_localizationKey);
        }

        private readonly string _localizationKey;
        public SpoutResolutionType Type { get; }

        public RProperty<string> Label { get; } = new RProperty<string>("");

        //NOTE: immutable arrayのほうが性質は良いのでそうしてもよい
        public static SpoutResolutionTypeNameViewModel[] AvailableItems { get; } = new SpoutResolutionTypeNameViewModel[]
        {
            new(SpoutResolutionType.SameAsWindow, "Window_SpoutOutput_Resolution_SameAsScreen"),
            new(SpoutResolutionType.Fixed1280, "Window_SpoutOutput_Resolution_Fixed1280"),
            new(SpoutResolutionType.Fixed1920, "Window_SpoutOutput_Resolution_Fixed1920"),
            new(SpoutResolutionType.Fixed2560, "Window_SpoutOutput_Resolution_Fixed2560"),
            new(SpoutResolutionType.Fixed3840, "Window_SpoutOutput_Resolution_Fixed3840"),
            new(SpoutResolutionType.Fixed1280Vertical, "Window_SpoutOutput_Resolution_Fixed1280Vertical"),
            new(SpoutResolutionType.Fixed1920Vertical, "Window_SpoutOutput_Resolution_Fixed1920Vertical"),
            new(SpoutResolutionType.Fixed2560Vertical, "Window_SpoutOutput_Resolution_Fixed2560Vertical"),
            new(SpoutResolutionType.Fixed3840Vertical, "Window_SpoutOutput_Resolution_Fixed3840Vertical"),
        };
    }
}
