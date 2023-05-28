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
        public bool HalfFpsMode { get; set; } = false;
        public bool UseFrameReductionEffect { get; set; } = false;
        
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
        public int ShadowIntensity { get; set; } = 65;
        public int ShadowYaw { get; set; } = -20;
        public int ShadowPitch { get; set; } = 8;
        public int ShadowDepthOffset { get; set; } = 40;

        #endregion

        #region Bloom

        public int BloomIntensity { get; set; } = 50;
        public int BloomThreshold { get; set; } = 100;

        public int BloomR { get; set; } = 255;
        public int BloomG { get; set; } = 255;
        public int BloomB { get; set; } = 255;

        #endregion

        #region Wind

        public bool EnableWind { get; set; } = true;
        public int WindStrength { get; set; } = 100;
        public int WindInterval { get; set; } = 100;
        public int WindYaw { get; set; } = 90;

        #endregion

    }

    public enum AntiAliasStyles
    {
        None = 0,
        Low = 1,
        Mid = 2,
        High = 3,
    }
}
