using Microsoft.Win32;
using System.IO;
using System.Windows.Media;

namespace Baku.VMagicMirrorConfig
{
    public class WindowSettingViewModel : SettingViewModelBase
    {
        internal WindowSettingViewModel(WindowSettingSync model, IMessageSender sender) : base(sender)
        {
            _model = model;

            void UpdatePickerColor() =>
                PickerColor = Color.FromRgb((byte)_model.R.Value, (byte)_model.G.Value, (byte)_model.B.Value);
            _model.R.PropertyChanged += (_, __) => UpdatePickerColor();
            _model.G.PropertyChanged += (_, __) => UpdatePickerColor();
            _model.B.PropertyChanged += (_, __) => UpdatePickerColor();

            BackgroundImageSetCommand = new ActionCommand(SetBackgroundImage);
            BackgroundImageClearCommand = new ActionCommand(
                () => _model.BackgroundImagePath.Value = ""
                );

            ResetBackgroundColorSettingCommand = new ActionCommand(
                () => SettingResetUtils.ResetSingleCategoryAsync(_model.ResetBackgroundColor)
                );
            ResetWindowPositionCommand = new ActionCommand(_model.ResetWindowPosition);
            ResetOpacitySettingCommand = new ActionCommand(
                () => SettingResetUtils.ResetSingleCategoryAsync(_model.ResetOpacity)
                );

            //初期値を反映しないと変な事になるので注意
            UpdatePickerColor();
        }

        private readonly WindowSettingSync _model;

        public RProperty<int> R => _model.R;
        public RProperty<int> G => _model.G;
        public RProperty<int> B => _model.B;

        private Color _pickerColor;
        /// <summary> ColorPickerに表示する、Alphaを考慮しない背景色を取得、設定します。 </summary>
        public Color PickerColor
        {
            get => _pickerColor;
            set
            {
                if (SetValue(ref _pickerColor, value))
                {
                    R.Value = PickerColor.R;
                    G.Value = PickerColor.G;
                    B.Value = PickerColor.B;
                }
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

        private void SetBackgroundImage()
        {
            //NOTE: 画像形式を絞らないと辛いのでAll Filesとかは無しです。
            var dialog = new OpenFileDialog()
            {
                Title = "Select Background Image",
                Filter = "Image files (*.png;*.jpg)|*.png;*.jpg",
                Multiselect = false,
            };

            if (dialog.ShowDialog() == true && File.Exists(dialog.FileName))
            {
                _model.BackgroundImagePath.Value = Path.GetFullPath(dialog.FileName);
            }
        }
    }
}
