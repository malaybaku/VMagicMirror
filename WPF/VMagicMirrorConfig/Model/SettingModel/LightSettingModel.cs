using System;

namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// エフェクト関連のモデル。ライト以外も扱っているが、歴史的経緯でライトということになっている
    /// </summary>
    class LightSettingModel : SettingModelBase<LightSetting>
    {
        public LightSettingModel() : this(ModelResolver.Instance.Resolve<IMessageSender>())
        {
        }

        public LightSettingModel(IMessageSender sender) : base(sender)
        {
            var s = LightSetting.Default;
            var factory = MessageFactory.Instance;

            //エフェクト関係は設定項目がシンプルなため、例外はほぼ無い(色関係のメッセージ送信がちょっと特殊なくらい)
            AntiAliasStyle = new RProperty<int>(s.AntiAliasStyle, i => SendMessage(factory.SetAntiAliasStyle(i)));
            HalfFpsMode = new RProperty<bool>(s.HalfFpsMode, v => SendMessage(factory.SetHalfFpsMode(v)));
            UseFrameReductionEffect = new RProperty<bool>(
                s.UseFrameReductionEffect, v => SendMessage(factory.UseFrameReductionEffect(v)));

            LightIntensity = new RProperty<int>(s.LightIntensity, i => SendMessage(factory.LightIntensity(i)));
            LightYaw = new RProperty<int>(s.LightYaw, i => SendMessage(factory.LightYaw(i)));
            LightPitch = new RProperty<int>(s.LightPitch, i => SendMessage(factory.LightPitch(i)));

            Action sendLightColor = () =>
                SendMessage(factory.LightColor(LightR?.Value ?? 255, LightG?.Value ?? 255, LightB?.Value ?? 255));
            LightR = new RProperty<int>(s.LightR, _ => sendLightColor());
            LightG = new RProperty<int>(s.LightG, _ => sendLightColor());
            LightB = new RProperty<int>(s.LightB, _ => sendLightColor());
            UseDesktopLightAdjust = new RProperty<bool>(s.UseDesktopLightAdjust, b => SendMessage(factory.UseDesktopLightAdjust(b)));

            EnableShadow = new RProperty<bool>(s.EnableShadow, b => SendMessage(factory.ShadowEnable(b)));
            ShadowIntensity = new RProperty<int>(s.ShadowIntensity, i => SendMessage(factory.ShadowIntensity(i)));
            ShadowYaw = new RProperty<int>(s.ShadowYaw, i => SendMessage(factory.ShadowYaw(i)));
            ShadowPitch = new RProperty<int>(s.ShadowPitch, i => SendMessage(factory.ShadowPitch(i)));
            ShadowDepthOffset = new RProperty<int>(s.ShadowDepthOffset, i => SendMessage(factory.ShadowDepthOffset(i)));

            BloomIntensity = new RProperty<int>(s.BloomIntensity, i => SendMessage(factory.BloomIntensity(i)));
            BloomThreshold = new RProperty<int>(s.BloomThreshold, i => SendMessage(factory.BloomThreshold(i)));
            Action sendBloomColor = () =>
                SendMessage(factory.BloomColor(BloomR?.Value ?? 255, BloomG?.Value ?? 255, BloomB?.Value ?? 255));
            BloomR = new RProperty<int>(s.BloomR, _ => sendBloomColor());
            BloomG = new RProperty<int>(s.BloomG, _ => sendBloomColor());
            BloomB = new RProperty<int>(s.BloomB, _ => sendBloomColor());

            EnableOutlineEffect = new RProperty<bool>(s.EnableOutlineEffect, v => SendMessage(factory.OutlineEffectEnable(v)));
            OutlineEffectThickness = new RProperty<int>(s.OutlineEffectThickness, v => SendMessage(factory.OutlineEffectThickness(v)));
            Action sendOutlineEffectColor = () =>
                SendMessage(factory.OutlineEffectColor(OutlineEffectR?.Value ?? 255, OutlineEffectG?.Value ?? 255, OutlineEffectB?.Value ?? 255));
            OutlineEffectR = new RProperty<int>(s.OutlineEffectR, _ => sendOutlineEffectColor());
            OutlineEffectG = new RProperty<int>(s.OutlineEffectG, _ => sendOutlineEffectColor());
            OutlineEffectB = new RProperty<int>(s.OutlineEffectB, _ => sendOutlineEffectColor());
            OutlineEffectHighQualityMode = new RProperty<bool>(
                s.OutlineEffectHighQualityMode,
                v => SendMessage(factory.OutlineEffectHighQualityMode(v))
                );

            EnableWind = new RProperty<bool>(s.EnableWind, b => SendMessage(factory.WindEnable(b)));
            WindStrength = new RProperty<int>(s.WindStrength, i => SendMessage(factory.WindStrength(i)));
            WindInterval = new RProperty<int>(s.WindInterval, i => SendMessage(factory.WindInterval(i)));
            WindYaw = new RProperty<int>(s.WindYaw, i => SendMessage(factory.WindYaw(i)));
        }

        #region Image Quality

        public RProperty<int> AntiAliasStyle { get; }
        public RProperty<bool> HalfFpsMode { get; }
        public RProperty<bool> UseFrameReductionEffect { get; }

        #endregion

        #region Light

        public RProperty<int> LightIntensity { get; }
        public RProperty<int> LightYaw { get; }
        public RProperty<int> LightPitch { get; }

        public RProperty<int> LightR { get; }
        public RProperty<int> LightG { get; }
        public RProperty<int> LightB { get; }

        public RProperty<bool> UseDesktopLightAdjust { get; }

        #endregion

        #region Shadow

        public RProperty<bool> EnableShadow { get; }
        public RProperty<int> ShadowIntensity { get; }
        public RProperty<int> ShadowYaw { get; }
        public RProperty<int> ShadowPitch { get; }
        public RProperty<int> ShadowDepthOffset { get; }

        #endregion

        #region Bloom

        public RProperty<int> BloomIntensity { get; }
        public RProperty<int> BloomThreshold { get; }

        public RProperty<int> BloomR { get; }
        public RProperty<int> BloomG { get; }
        public RProperty<int> BloomB { get; }

        #endregion

        #region OutlineEffect

        public RProperty<bool> EnableOutlineEffect { get; }
        public RProperty<int> OutlineEffectThickness { get; }
        public RProperty<bool> OutlineEffectHighQualityMode { get; }

        public RProperty<int> OutlineEffectR { get; }
        public RProperty<int> OutlineEffectG { get; }
        public RProperty<int> OutlineEffectB { get; }

        #endregion

        #region Wind

        public RProperty<bool> EnableWind { get; }
        public RProperty<int> WindStrength { get; }
        public RProperty<int> WindInterval { get; }
        public RProperty<int> WindYaw { get; }

        #endregion

        #region Reset API

        /// <summary>
        /// Unity側で画質をデフォルトにリセットさせたのち、そのリセット後の画質の名称を適用します。
        /// </summary>
        /// <returns></returns>
        public void ResetImageQuality()
        {
            HalfFpsMode.Value = false;
            UseFrameReductionEffect.Value = false;
        }

        public void ResetLightSetting()
        {
            var setting = LightSetting.Default;
            LightR.Value = setting.LightR;
            LightG.Value = setting.LightG;
            LightB.Value = setting.LightB;
            LightIntensity.Value = setting.LightIntensity;
            LightYaw.Value = setting.LightYaw;
            LightPitch.Value = setting.LightPitch;
        }

        public void ResetShadowSetting()
        {
            var setting = LightSetting.Default;
            EnableShadow.Value = setting.EnableShadow;
            ShadowIntensity.Value = setting.ShadowIntensity;
            ShadowYaw.Value = setting.ShadowYaw;
            ShadowPitch.Value = setting.ShadowPitch;
            ShadowDepthOffset.Value = setting.ShadowDepthOffset;
        }

        public void ResetBloomSetting()
        {
            var setting = LightSetting.Default;
            BloomR.Value = setting.BloomR;
            BloomG.Value = setting.BloomG;
            BloomB.Value = setting.BloomB;
            BloomIntensity.Value = setting.BloomIntensity;
            BloomThreshold.Value = setting.BloomThreshold;
        }

        public void ResetOutlineEffectSetting()
        {
            var setting = LightSetting.Default;
            EnableOutlineEffect.Value = setting.EnableOutlineEffect;
            OutlineEffectThickness.Value = setting.OutlineEffectThickness;
            OutlineEffectR.Value = setting.OutlineEffectR;
            OutlineEffectG.Value = setting.OutlineEffectG;
            OutlineEffectB.Value = setting.OutlineEffectB;
            OutlineEffectHighQualityMode.Value = setting.OutlineEffectHighQualityMode;
        }

        public void ResetWindSetting()
        {
            var setting = LightSetting.Default;
            EnableWind.Value = setting.EnableWind;
            WindStrength.Value = setting.WindStrength;
            WindInterval.Value = setting.WindInterval;
            WindYaw.Value = setting.WindYaw;
        }

        public override void ResetToDefault()
        {
            ResetLightSetting();
            ResetShadowSetting();
            ResetBloomSetting();
            ResetOutlineEffectSetting();
            ResetWindSetting();
            ResetImageQuality();
        }

        #endregion

    }
}
