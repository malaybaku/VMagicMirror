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
            ResetAmbientOcclusionSettingCommand = new ActionCommand(
                () => SettingResetUtils.ResetSingleCategoryAsync(_model.ResetAmbientOcclusionSetting)
                );
            ResetBloomSettingCommand = new ActionCommand(
                () => SettingResetUtils.ResetSingleCategoryAsync(_model.ResetBloomSetting)
                );
            ResetOutlineEffectSettingCommand = new ActionCommand(
                () => SettingResetUtils.ResetSingleCategoryAsync(_model.ResetOutlineEffectSetting)
                );
            ResetRimSettingCommand = new ActionCommand(
                () => SettingResetUtils.ResetSingleCategoryAsync(_model.ResetRimSetting)
                );
            ResetWindSettingCommand = new ActionCommand(
                () => SettingResetUtils.ResetSingleCategoryAsync(_model.ResetWindSetting)
                );
            ResetImageQualitySettingCommand = new ActionCommand(ResetImageQuality);
            EditLightColorCommand = new ActionCommand(() => ShowColorWindow(
                new RgbColorBinding(LightR, LightG, LightB),
                "Light_Color"
            ));
            EditShadowColorCommand = new ActionCommand(() => ShowColorWindow(
                new RgbColorBinding(ShadowR, ShadowG, ShadowB),
                "Shadow_Color"
            ));
            EditBloomColorCommand = new ActionCommand(() => ShowColorWindow(
                new RgbColorBinding(BloomR, BloomG, BloomB),
                "Bloom_Color"
            ));
            EditAmbientOcclusionColorCommand = new ActionCommand(() => ShowColorWindow(
                new RgbColorBinding(AmbientOcclusionR, AmbientOcclusionG, AmbientOcclusionB),
                "AO_Color"
            ));
            EditOutlineEffectColorCommand = new ActionCommand(() => ShowColorWindow(
                new RgbColorBinding(OutlineEffectR, OutlineEffectG, OutlineEffectB),
                "OutlineEffect_Color"
            ));
            EditRimColorCommand = new ActionCommand(() => ShowColorWindow(
                new RgbColorBinding(RimR, RimG, RimB),
                "RimEffect_Color"
            ));

            if (IsInDesignMode)
            {
                AntiAliasStyle = new RProperty<AntiAliasStyles>(AntiAliasStyles.None);
                TargetFramerateStyle = new RProperty<TargetFramerateStyles>(TargetFramerateStyles.Fixed60);
            }
            else
            {
                AntiAliasStyle = new RProperty<AntiAliasStyles>(
                    GetAntiAliasStyle(_model.AntiAliasStyle.Value),
                    value => _model.AntiAliasStyle.Value = (int)value
                    );
                TargetFramerateStyle = new RProperty<TargetFramerateStyles>(
                    (TargetFramerateStyles) _model.TargetFramerateStyle.Value,
                    value => _model.TargetFramerateStyle.Value = (int) value
                    );

                model.AntiAliasStyle.AddWeakEventHandler(ApplyAntiAliasStyle);
                model.TargetFramerateStyle.AddWeakEventHandler(ApplyTargetFramerate);

                model.LightR.AddWeakEventHandler(UpdateLightColor);
                model.LightG.AddWeakEventHandler(UpdateLightColor);
                model.LightB.AddWeakEventHandler(UpdateLightColor);

                model.ShadowR.AddWeakEventHandler(UpdateShadowColor);
                model.ShadowG.AddWeakEventHandler(UpdateShadowColor);
                model.ShadowB.AddWeakEventHandler(UpdateShadowColor);

                model.BloomR.AddWeakEventHandler(UpdateBloomColor);
                model.BloomG.AddWeakEventHandler(UpdateBloomColor);
                model.BloomB.AddWeakEventHandler(UpdateBloomColor);

                model.AmbientOcclusionR.AddWeakEventHandler(UpdateAmbientOcclusionColor);
                model.AmbientOcclusionG.AddWeakEventHandler(UpdateAmbientOcclusionColor);
                model.AmbientOcclusionB.AddWeakEventHandler(UpdateAmbientOcclusionColor);

                model.OutlineEffectR.AddWeakEventHandler(UpdateOutlineEffectColor);
                model.OutlineEffectG.AddWeakEventHandler(UpdateOutlineEffectColor);
                model.OutlineEffectB.AddWeakEventHandler(UpdateOutlineEffectColor);

                model.RimR.AddWeakEventHandler(UpdateRimColor);
                model.RimG.AddWeakEventHandler(UpdateRimColor);
                model.RimB.AddWeakEventHandler(UpdateRimColor);
            }
        }

        private readonly LightSettingModel _model;
        private readonly ImageQualitySetting _imageQuality;

        public RProperty<string> ImageQuality => _imageQuality.ImageQuality;
        public ReadOnlyObservableCollection<string> ImageQualityNames => _imageQuality.ImageQualityNames;

        public RProperty<AntiAliasStyles> AntiAliasStyle { get; }
        public AntiAliasStyles[] AvailableAntiAliasStyle { get; } =
        [
            AntiAliasStyles.None,
            AntiAliasStyles.Low,
            AntiAliasStyles.Mid,
            AntiAliasStyles.High,
        ];
        public RProperty<TargetFramerateStyles> TargetFramerateStyle { get; }
        public TargetFramerateStyles[] AvailableTargetFramerateStyle { get; } =
        [
            TargetFramerateStyles.Fixed60,
            TargetFramerateStyles.Fixed30,
            TargetFramerateStyles.UseVSync,
        ];

        public RProperty<bool> UseFrameReductionEffect => _model.UseFrameReductionEffect;
        public RProperty<bool> DisableHdrAlways => _model.DisableHdrAlways;

        void UpdateLightColor(object? sender, PropertyChangedEventArgs e) => RaisePropertyChanged(nameof(LightColor));
        void UpdateShadowColor(object? sender, PropertyChangedEventArgs e) => RaisePropertyChanged(nameof(ShadowColor));
        void UpdateBloomColor(object? sender, PropertyChangedEventArgs e) => RaisePropertyChanged(nameof(BloomColor));
        void UpdateAmbientOcclusionColor(object? sender, PropertyChangedEventArgs e) => RaisePropertyChanged(nameof(AmbientOcclusionColor));
        void UpdateOutlineEffectColor(object? sender, PropertyChangedEventArgs e) => RaisePropertyChanged(nameof(OutlineEffectColor));
        void UpdateRimColor(object? sender, PropertyChangedEventArgs e) => RaisePropertyChanged(nameof(RimColor));

        void ApplyAntiAliasStyle(object? sender, PropertyChangedEventArgs e) 
            => AntiAliasStyle.Value = GetAntiAliasStyle(_model.AntiAliasStyle.Value);
        void ApplyTargetFramerate(object? sender, PropertyChangedEventArgs e)
            => TargetFramerateStyle.Value = GetTargetFramerate(_model.TargetFramerateStyle.Value);

        static AntiAliasStyles GetAntiAliasStyle(int value)
        {
            if (value >= 0 && value <= (int)AntiAliasStyles.High)
            {
                return (AntiAliasStyles)value;
            }
            else
            {
                return AntiAliasStyles.None;
            }
        }

        static TargetFramerateStyles GetTargetFramerate(int value)
        {
            if (value >= 0 && value <= (int)TargetFramerateStyles.UseVSync)
            {
                return (TargetFramerateStyles)value;
            }
            else
            {
                return TargetFramerateStyles.Fixed60;
            }
        }

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
        public RProperty<int> ShadowR => _model.ShadowR;
        public RProperty<int> ShadowG => _model.ShadowG;
        public RProperty<int> ShadowB => _model.ShadowB;
        public RProperty<int> ShadowBlur => _model.ShadowBlur;
        public RProperty<int> ShadowIntensity => _model.ShadowIntensity;
        public RProperty<int> ShadowYaw => _model.ShadowYaw;
        public RProperty<int> ShadowPitch => _model.ShadowPitch;
        public RProperty<int> ShadowDepthOffset => _model.ShadowDepthOffset;

        public Color ShadowColor
        {
            get => Color.FromRgb((byte)ShadowR.Value, (byte)ShadowG.Value, (byte)ShadowB.Value);
            set
            {
                ShadowR.Value = value.R;
                ShadowG.Value = value.G;
                ShadowB.Value = value.B;
            }
        }

        public RProperty<bool> EnableFixedShadowAlways => _model.EnableFixedShadowAlways;
        public RProperty<bool> EnableFixedShadowWhenLocomotionActive => _model.EnableFixedShadowWhenLocomotionActive;

        #endregion

        #region Ambient Occlusion

        public RProperty<bool> EnableAmbientOcclusion => _model.EnableAmbientOcclusion;
        public RProperty<int> AmbientOcclusionIntensity => _model.AmbientOcclusionIntensity;
        public RProperty<int> AmbientOcclusionR => _model.AmbientOcclusionR;
        public RProperty<int> AmbientOcclusionG => _model.AmbientOcclusionG;
        public RProperty<int> AmbientOcclusionB => _model.AmbientOcclusionB;

        public Color AmbientOcclusionColor
        {
            get => Color.FromRgb((byte)AmbientOcclusionR.Value, (byte)AmbientOcclusionG.Value, (byte)AmbientOcclusionB.Value);
            set
            {
                AmbientOcclusionR.Value = value.R;
                AmbientOcclusionG.Value = value.G;
                AmbientOcclusionB.Value = value.B;
            }
        }

        #endregion

        #region Bloom

        public RProperty<bool> EnableBloom => _model.EnableBloom;
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

        #region OutlineEffect

        public RProperty<bool> EnableOutlineEffect => _model.EnableOutlineEffect;
        public RProperty<int> OutlineEffectThickness => _model.OutlineEffectThickness;
        public RProperty<int> OutlineEffectR => _model.OutlineEffectR;
        public RProperty<int> OutlineEffectG => _model.OutlineEffectG;
        public RProperty<int> OutlineEffectB => _model.OutlineEffectB;
        public RProperty<bool> OutlineEffectHighQualityMode => _model.OutlineEffectHighQualityMode;

        public Color OutlineEffectColor
        {
            get => Color.FromRgb((byte)OutlineEffectR.Value, (byte)OutlineEffectG.Value, (byte)OutlineEffectB.Value);
            set
            {
                OutlineEffectR.Value = value.R;
                OutlineEffectG.Value = value.G;
                OutlineEffectB.Value = value.B;
            }
        }

        #endregion

        #region Rim

        public RProperty<bool> RimEnabled => _model.RimEnabled;
        public RProperty<int> RimIntensity => _model.RimIntensity;
        public RProperty<int> RimThickness => _model.RimThickness;
        public RProperty<int> RimAngle => _model.RimAngle;
        public RProperty<int> RimR => _model.RimR;
        public RProperty<int> RimG => _model.RimG;
        public RProperty<int> RimB => _model.RimB;
        public RProperty<int> RimHdrColorIntensity => _model.RimHdrColorIntensity;

        public Color RimColor
        {
            get => Color.FromRgb((byte)RimR.Value, (byte)RimG.Value, (byte)RimB.Value);
            set
            {
                RimR.Value = value.R;
                RimG.Value = value.G;
                RimB.Value = value.B;
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

        public ActionCommand EditLightColorCommand { get; }
        public ActionCommand EditShadowColorCommand { get; }
        public ActionCommand EditBloomColorCommand { get; }
        public ActionCommand EditAmbientOcclusionColorCommand { get; }
        public ActionCommand EditOutlineEffectColorCommand { get; }
        public ActionCommand EditRimColorCommand { get; }

        public ActionCommand ResetLightSettingCommand { get; }
        public ActionCommand ResetShadowSettingCommand { get; }
        public ActionCommand ResetAmbientOcclusionSettingCommand { get; }
        public ActionCommand ResetBloomSettingCommand { get; }
        public ActionCommand ResetOutlineEffectSettingCommand { get; }
        public ActionCommand ResetRimSettingCommand { get; }
        public ActionCommand ResetWindSettingCommand { get; }

        private async void ResetImageQuality()
        {
            _model.ResetImageQuality();
            await _imageQuality.ResetAsync();
        }

        private static void ShowColorWindow(RgbColorBinding rgb, string titleResourceKey)
        {
            View.ColorEditWindow.ShowColorWindow(
                View.SettingWindow.CurrentWindow,
                rgb,
                LocalizedString.GetString(titleResourceKey)
            );
        }
    }
}
