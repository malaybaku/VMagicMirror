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

            if (IsInDegignMode)
            {
                return;
            }

            _model.R.AddWeakEventHandler(OnPickerColorChanged);
            _model.G.AddWeakEventHandler(OnPickerColorChanged);
            _model.B.AddWeakEventHandler(OnPickerColorChanged);
            //初期値を反映しないと変な事になるので注意
            RaisePropertyChanged(nameof(PickerColor));
        }

        private readonly WindowSettingModel _model;

        public RProperty<int> R => _model.R;
        public RProperty<int> G => _model.G;
        public RProperty<int> B => _model.B;

        private void OnPickerColorChanged(object? sender, PropertyChangedEventArgs e)
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

        public RProperty<bool> IsTransparent => _model.IsTransparent;
        public RProperty<bool> WindowDraggable => _model.WindowDraggable;
        public RProperty<bool> TopMost => _model.TopMost;

        public RProperty<int> WholeWindowTransparencyLevel => _model.WholeWindowTransparencyLevel;
        public RProperty<int> AlphaValueOnTransparent => _model.AlphaValueOnTransparent;

        public ActionCommand BackgroundImageSetCommand { get; }
        public ActionCommand BackgroundImageClearCommand { get; }

        public ActionCommand ResetWindowPositionCommand { get; }
        public ActionCommand ResetBackgroundColorSettingCommand { get; }
        public ActionCommand ResetOpacitySettingCommand { get; }       
    }
}
