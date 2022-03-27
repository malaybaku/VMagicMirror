using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    /// <summary>
    /// NOTE: Model側は"Effect"ではなく"Light"という言い方をしているが、
    /// これは設定ファイル側の命名の歴史的経緯に配慮しているためなので、Model側をノリで変更してはならない
    /// </summary>
    public class EffectSettingViewModel : SettingViewModelBase
    {
        public EffectSettingViewModel() : this(
            ModelResolver.Instance.Resolve<LightSettingModel>(),
            ModelResolver.Instance.Resolve<ImageQualitySetting>()
            )
        {
        }

        internal EffectSettingViewModel(LightSettingModel model, ImageQualitySetting imageQuality)
        {
            _model = model;
            _imageQuality = imageQuality;

            ResetLightSettingCommand = new ActionCommand(
                () => SettingResetUtils.ResetSingleCategoryAsync(async () =>
                {
                    model.ResetLightSetting();
                    await imageQuality.ResetAsync();
                }));
            ResetShadowSettingCommand = new ActionCommand(
                () => SettingResetUtils.ResetSingleCategoryAsync(_model.ResetShadowSetting)
                );
            ResetBloomSettingCommand = new ActionCommand(
                () => SettingResetUtils.ResetSingleCategoryAsync(_model.ResetBloomSetting)
                );
            ResetWindSettingCommand = new ActionCommand(
                () => SettingResetUtils.ResetSingleCategoryAsync(_model.ResetWindSetting)
                );
            ResetImageQualitySettingCommand = new ActionCommand(ResetImageQuality);

            if (!IsInDegignMode)
            {
                model.LightR.AddWeakEventHandler(UpdateLightColor);
                model.LightG.AddWeakEventHandler(UpdateLightColor);
                model.LightB.AddWeakEventHandler(UpdateLightColor);

                model.BloomR.AddWeakEventHandler(UpdateBloomColor);
                model.BloomG.AddWeakEventHandler(UpdateBloomColor);
                model.BloomB.AddWeakEventHandler(UpdateBloomColor);
            }
        }

        private readonly LightSettingModel _model;
        private readonly ImageQualitySetting _imageQuality;

        public RProperty<string> ImageQuality => _imageQuality.ImageQuality;
        public ReadOnlyObservableCollection<string> ImageQualityNames => _imageQuality.ImageQualityNames;
        public RProperty<bool> HalfFpsMode => _model.HalfFpsMode;

        void UpdateLightColor(object? sender, PropertyChangedEventArgs e) => RaisePropertyChanged(nameof(LightColor));
        void UpdateBloomColor(object? sender, PropertyChangedEventArgs e) => RaisePropertyChanged(nameof(BloomColor));

        #region Light

        public RProperty<int> LightIntensity => _model.LightIntensity;
        public RProperty<int> LightYaw => _model.LightYaw;
        public RProperty<int> LightPitch => _model.LightPitch;

        public RProperty<int> LightR => _model.LightR;
        public RProperty<int> LightG => _model.LightG;
        public RProperty<int> LightB => _model.LightB;

        public Color LightColor
        {
            get => Color.FromRgb((byte)LightR.Value, (byte)LightG.Value, (byte)LightB.Value);
            set
            {
                LightR.Value = value.R;
                LightG.Value = value.G;
                LightB.Value = value.B;
            }
        }

        public RProperty<bool> UseDesktopLightAdjust => _model.UseDesktopLightAdjust;

        //NOTE: 色が変わったら表示を追従させるだけでいいのがポイント。メッセージ送信自体はモデル側で行う
        private void UpdateLightColor()
            => LightColor = Color.FromRgb((byte)LightR.Value, (byte)LightG.Value, (byte)LightB.Value);

        #endregion

        #region Shadow

        public RProperty<bool> EnableShadow => _model.EnableShadow;
        public RProperty<int> ShadowIntensity => _model.ShadowIntensity;
        public RProperty<int> ShadowYaw => _model.ShadowYaw;
        public RProperty<int> ShadowPitch => _model.ShadowPitch;
        public RProperty<int> ShadowDepthOffset => _model.ShadowDepthOffset;

        #endregion

        #region Bloom

        public RProperty<int> BloomIntensity => _model.BloomIntensity;
        public RProperty<int> BloomThreshold => _model.BloomThreshold;

        public RProperty<int> BloomR => _model.BloomR;
        public RProperty<int> BloomG => _model.BloomG;
        public RProperty<int> BloomB => _model.BloomB;

        public Color BloomColor
        {
            get => Color.FromRgb((byte)BloomR.Value, (byte)BloomG.Value, (byte)BloomB.Value);
            set
            {
                BloomR.Value = value.R;
                BloomG.Value = value.G;
                BloomB.Value = value.B;
            }
        }

        #endregion

        #region Wind

        public RProperty<bool> EnableWind => _model.EnableWind;
        public RProperty<int> WindStrength => _model.WindStrength;
        public RProperty<int> WindInterval => _model.WindInterval;
        public RProperty<int> WindYaw => _model.WindYaw;

        #endregion

        public ActionCommand ResetImageQualitySettingCommand { get; }

        public ActionCommand ResetLightSettingCommand { get; }
        public ActionCommand ResetShadowSettingCommand { get; }
        public ActionCommand ResetBloomSettingCommand { get; }
        public ActionCommand ResetWindSettingCommand { get; }

        private async void ResetImageQuality()
        {
            _model.ResetImageQuality();
            await _imageQuality.ResetAsync();
        }
    }
}
