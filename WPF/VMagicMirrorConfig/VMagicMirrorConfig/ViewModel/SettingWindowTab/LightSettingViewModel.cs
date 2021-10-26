using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// ライトと言ってるが、実際にはタイピングエフェクトを除いたエフェクトっぽい色々の設定を持ってるクラス。
    /// クラス名やメンバー名はヘタに変えると旧バージョンの設定が読めなくなるので、変更時は要注意。
    /// (たしかクラス名は改変可だが、勢いでSaveDataクラスまでリファクタリングすると後方互換でなくなってしまう)
    /// </summary>
    public class LightSettingViewModel : SettingViewModelBase
    {
        internal LightSettingViewModel(LightSettingSync model, IMessageSender sender) : base(sender)
        {
            _model = model;

            _lightColor = Color.FromRgb((byte)model.LightR.Value, (byte)model.LightG.Value, (byte)model.LightB.Value);
            model.LightR.PropertyChanged += (_, __) => UpdateLightColor();
            model.LightG.PropertyChanged += (_, __) => UpdateLightColor();
            model.LightB.PropertyChanged += (_, __) => UpdateLightColor();

            _bloomColor = Color.FromRgb((byte)model.BloomR.Value, (byte)model.BloomG.Value, (byte)model.BloomB.Value);
            model.BloomR.PropertyChanged += (_, __) => UpdateBloomColor();
            model.BloomG.PropertyChanged += (_, __) => UpdateBloomColor();
            model.BloomB.PropertyChanged += (_, __) => UpdateBloomColor();

            ResetLightSettingCommand = new ActionCommand(
                () => SettingResetUtils.ResetSingleCategoryAsync(model.ResetLightSetting)
                );
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

            //最初の時点で不整合しなければ後は何でもOK
            UpdateLightColor();
            UpdateBloomColor();
        }

        private readonly LightSettingSync _model;

        public async Task InitializeQualitySelectionsAsync()
            => await _model.InitializeQualitySelectionsAsync();

        public RProperty<string> ImageQuality => _model.ImageQuality;
        public ReadOnlyObservableCollection<string> ImageQualityNames => _model.ImageQualityNames;
        public RProperty<bool> HalfFpsMode => _model.HalfFpsMode;

        #region Light

        public RProperty<int> LightIntensity => _model.LightIntensity;
        public RProperty<int> LightYaw => _model.LightYaw;
        public RProperty<int> LightPitch => _model.LightPitch;

        public RProperty<int> LightR => _model.LightR;
        public RProperty<int> LightG => _model.LightG;
        public RProperty<int> LightB => _model.LightB;

        private Color _lightColor;
        public Color LightColor
        {
            get => _lightColor;
            set
            {
                if (SetValue(ref _lightColor, value))
                {
                    LightR.Value = value.R;
                    LightG.Value = value.G;
                    LightB.Value = value.B;
                }
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

        private Color _bloomColor;
        public Color BloomColor
        {
            get => _bloomColor;
            set
            {
                if (SetValue(ref _bloomColor, value))
                {
                    BloomR.Value = value.R;
                    BloomG.Value = value.G;
                    BloomB.Value = value.B;
                }
            }
        }

        private void UpdateBloomColor()
            => BloomColor = Color.FromRgb((byte)BloomR.Value, (byte)BloomG.Value, (byte)BloomB.Value);

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

        private void ResetImageQuality()
        {
            SettingResetUtils.ResetSingleCategoryAsync(
                async () => await _model.ResetImageQualityAsync()
                );
        }
    }
}
