namespace Baku.VMagicMirrorConfig
{
    /// <summary>XMLシリアライズを想定した、エフェクト系設定のエンティティ。</summary>
    /// <remarks>UI上はEffectと表示される項目なんですが、歴史的経緯でLightという名称を使っています。</remarks>
    public class LightSetting : SettingEntityBase
    {
        /// <summary>
        /// NOTE: 規約としてこの値は書き換えません。
        /// デフォルト値を参照したい人が、プロパティ読み込みのみの為だけに使います。
        /// </summary>
        public static LightSetting Default { get; } = new LightSetting();

        #region Image Quality

        public int AntiAliasStyle { get; set; } = 0;
        public int TargetFramerateStyle { get; set; } = 0;
        public bool UseFrameReductionEffect { get; set; } = false;
        public bool DisableHdrAlways { get; set; } = false;
        
        #endregion

        #region Light

        public int LightIntensity { get; set; } = 100;
        public int LightYaw { get; set; } = -30;
        public int LightPitch { get; set; } = 50;

        public int LightR { get; set; } = 255;
        public int LightG { get; set; } = 255;
        public int LightB { get; set; } = 255;

        public bool UseDesktopLightAdjust { get; set; } = false;

        #endregion

        #region Shadow

        public bool EnableShadow { get; set; } = true;
        public int ShadowR { get; set; } = 0;
        public int ShadowG { get; set; } = 0;
        public int ShadowB { get; set; } = 0;
        public int ShadowBlur { get; set; } = 10;
        public int ShadowIntensity { get; set; } = 65;
        public int ShadowYaw { get; set; } = -20;
        public int ShadowPitch { get; set; } = 8;
        public int ShadowDepthOffset { get; set; } = 40;

        public bool EnableFixedShadowAlways { get; set; } = false;
        public bool EnableFixedShadowWhenLocomotionActive { get; set; } = true;

        #endregion

        #region Ambient Occlusion

        public bool EnableAmbientOcclusion { get; set; } = false;
        public int AmbientOcclusionIntensity { get; set; } = 15;
        public int AmbientOcclusionR { get; set; } = 0;
        public int AmbientOcclusionG { get; set; } = 0;
        public int AmbientOcclusionB { get; set; } = 0;

        #endregion

        #region Bloom

        public bool EnableBloom { get; set; } = true;
        public int BloomIntensity { get; set; } = 50;
        public int BloomThreshold { get; set; } = 100;

        public int BloomR { get; set; } = 255;
        public int BloomG { get; set; } = 255;
        public int BloomB { get; set; } = 255;

        #endregion

        #region Outline Effect

        public bool EnableOutlineEffect { get; set; } = false;
        public int OutlineEffectThickness { get; set; } = 20;
        public int OutlineEffectR { get; set; } = 255;
        public int OutlineEffectG { get; set; } = 255;
        public int OutlineEffectB { get; set; } = 255;
        public bool OutlineEffectHighQualityMode { get; set; } = false;

        #endregion

        #region Rim Effect

        public bool RimEnabled { get; set; } = false;
        public int RimHdrColorIntensity { get; set; } = 10;
        // IntensityかThicknessどちらかが0の場合、Enabled=trueであっても実質的に無効になる
        public int RimIntensity { get; set; } = 100;
        // Rimの太さを表す無次元量で、OutlineThicknessと似てるがスケールが異なる
        public int RimThickness { get; set; } = 10;
        // 画面の上を基準として反時計回りのdegree
        public int RimAngle { get; set; } = 15;

        public int RimR { get; set; } = 255;
        public int RimG { get; set; } = 255;
        public int RimB { get; set; } = 255;

        #endregion

        #region Wind

        public bool EnableWind { get; set; } = true;
        public int WindStrength { get; set; } = 100;
        public int WindInterval { get; set; } = 100;
        public int WindYaw { get; set; } = 90;

        #endregion

    }

    public enum TargetFramerateStyles
    {
        Fixed60 = 0,
        Fixed30 = 1,
        UseVSync = 2,
    }
    public static class TargetFramerateStylesExtensions
    {
        public static int ToFramerate(this TargetFramerateStyles style) => style switch
        {
            TargetFramerateStyles.Fixed60 => 60,
            TargetFramerateStyles.Fixed30 => 30,
            TargetFramerateStyles.UseVSync => 0,
            _ => 60,
        };
    }

    public enum AntiAliasStyles
    {
        None = 0,
        Low = 1,
        Mid = 2,
        High = 3,
    }
}
