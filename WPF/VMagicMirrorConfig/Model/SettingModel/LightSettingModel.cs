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

            //エフェクト関係は設定項目がシンプルなため、例外はほぼ無い(色関係のメッセージ送信がちょっと特殊なくらい)
            AntiAliasStyle = new RProperty<int>(s.AntiAliasStyle, i => SendMessage(MessageFactory.SetAntiAliasStyle(i)));
            TargetFramerateStyle = new RProperty<int>(s.TargetFramerateStyle, i =>
            {
                var framerate = ((TargetFramerateStyles)i).ToFramerate();
                SendMessage(MessageFactory.SetTargetFramerate(framerate));
            });
            UseFrameReductionEffect = new RProperty<bool>(
                s.UseFrameReductionEffect, v => SendMessage(MessageFactory.UseFrameReductionEffect(v)));
            DisableHdrAlways = new RProperty<bool>(
                s.DisableHdrAlways, v => SendMessage(MessageFactory.DisableHdrAlways(v)));

            LightIntensity = new RProperty<int>(s.LightIntensity, i => SendMessage(MessageFactory.LightIntensity(i)));
            LightYaw = new RProperty<int>(s.LightYaw, i => SendMessage(MessageFactory.LightYaw(i)));
            LightPitch = new RProperty<int>(s.LightPitch, i => SendMessage(MessageFactory.LightPitch(i)));

            Action sendLightColor = () =>
                SendMessage(MessageFactory.LightColor(LightR?.Value ?? 255, LightG?.Value ?? 255, LightB?.Value ?? 255));
            LightR = new RProperty<int>(s.LightR, _ => sendLightColor());
            LightG = new RProperty<int>(s.LightG, _ => sendLightColor());
            LightB = new RProperty<int>(s.LightB, _ => sendLightColor());
            UseDesktopLightAdjust = new RProperty<bool>(s.UseDesktopLightAdjust, b => SendMessage(MessageFactory.UseDesktopLightAdjust(b)));

            EnableShadow = new RProperty<bool>(s.EnableShadow, b => SendMessage(MessageFactory.ShadowEnable(b)));
            Action sendShadowColor = () =>
                SendMessage(MessageFactory.ShadowColor(ShadowR?.Value ?? 0, ShadowG?.Value ?? 0, ShadowB?.Value ?? 0));
            ShadowR = new RProperty<int>(s.ShadowR, _ => sendShadowColor());
            ShadowG = new RProperty<int>(s.ShadowG, _ => sendShadowColor());
            ShadowB = new RProperty<int>(s.ShadowB, _ => sendShadowColor());
            ShadowBlur = new RProperty<int>(s.ShadowBlur, i => SendMessage(MessageFactory.ShadowBlur(i)));
            ShadowIntensity = new RProperty<int>(s.ShadowIntensity, i => SendMessage(MessageFactory.ShadowIntensity(i)));
            ShadowYaw = new RProperty<int>(s.ShadowYaw, i => SendMessage(MessageFactory.ShadowYaw(i)));
            ShadowPitch = new RProperty<int>(s.ShadowPitch, i => SendMessage(MessageFactory.ShadowPitch(i)));
            ShadowDepthOffset = new RProperty<int>(s.ShadowDepthOffset, i => SendMessage(MessageFactory.ShadowDepthOffset(i)));

            EnableFixedShadowAlways = new RProperty<bool>(
                s.EnableFixedShadowAlways,
                v => SendMessage(MessageFactory.FixedShadowAlwaysEnable(v))
                );
            EnableFixedShadowWhenLocomotionActive = new RProperty<bool>(
                s.EnableFixedShadowWhenLocomotionActive,
                v => SendMessage(MessageFactory.FixedShadowWhenLocomotionActiveEnable(v))
                );

            EnableBloom = new RProperty<bool>(s.EnableBloom, b => SendMessage(MessageFactory.BloomEnable(b)));
            BloomIntensity = new RProperty<int>(s.BloomIntensity, i => SendMessage(MessageFactory.BloomIntensity(i)));
            BloomThreshold = new RProperty<int>(s.BloomThreshold, i => SendMessage(MessageFactory.BloomThreshold(i)));
            Action sendBloomColor = () =>
                SendMessage(MessageFactory.BloomColor(BloomR?.Value ?? 255, BloomG?.Value ?? 255, BloomB?.Value ?? 255));
            BloomR = new RProperty<int>(s.BloomR, _ => sendBloomColor());
            BloomG = new RProperty<int>(s.BloomG, _ => sendBloomColor());
            BloomB = new RProperty<int>(s.BloomB, _ => sendBloomColor());

            EnableOutlineEffect = new RProperty<bool>(s.EnableOutlineEffect, v => SendMessage(MessageFactory.OutlineEffectEnable(v)));
            OutlineEffectThickness = new RProperty<int>(s.OutlineEffectThickness, v => SendMessage(MessageFactory.OutlineEffectThickness(v)));
            Action sendOutlineEffectColor = () =>
                SendMessage(MessageFactory.OutlineEffectColor(OutlineEffectR?.Value ?? 255, OutlineEffectG?.Value ?? 255, OutlineEffectB?.Value ?? 255));
            OutlineEffectR = new RProperty<int>(s.OutlineEffectR, _ => sendOutlineEffectColor());
            OutlineEffectG = new RProperty<int>(s.OutlineEffectG, _ => sendOutlineEffectColor());
            OutlineEffectB = new RProperty<int>(s.OutlineEffectB, _ => sendOutlineEffectColor());
            OutlineEffectHighQualityMode = new RProperty<bool>(
                s.OutlineEffectHighQualityMode,
                v => SendMessage(MessageFactory.OutlineEffectHighQualityMode(v))
                );

            RimEnabled = new RProperty<bool>(s.RimEnabled, v => SendMessage(MessageFactory.SetRimEnabled(v)));
            RimIntensity = new RProperty<int>(s.RimIntensity, i => SendMessage(MessageFactory.SetRimIntensity(i)));
            RimThickness = new RProperty<int>(s.RimThickness, i => SendMessage(MessageFactory.SetRimThickness(i)));
            RimAngle = new RProperty<int>(s.RimAngle, i => SendMessage(MessageFactory.SetRimAngle(i)));
            Action sendRimColor = () =>
                SendMessage(MessageFactory.SetRimColor(RimR?.Value ?? 255, RimG?.Value ?? 255, RimB?.Value ?? 255));
            RimR = new RProperty<int>(s.RimR, _ => sendRimColor());
            RimG = new RProperty<int>(s.RimG, _ => sendRimColor());
            RimB = new RProperty<int>(s.RimB, _ => sendRimColor());
            RimHdrColorIntensity = new RProperty<int>(s.RimHdrColorIntensity, i => SendMessage(MessageFactory.SetRimHdrColorIntensity(i)));

            EnableWind = new RProperty<bool>(s.EnableWind, b => SendMessage(MessageFactory.WindEnable(b)));
            WindStrength = new RProperty<int>(s.WindStrength, i => SendMessage(MessageFactory.WindStrength(i)));
            WindInterval = new RProperty<int>(s.WindInterval, i => SendMessage(MessageFactory.WindInterval(i)));
            WindYaw = new RProperty<int>(s.WindYaw, i => SendMessage(MessageFactory.WindYaw(i)));

            EnableAmbientOcclusion = new RProperty<bool>(s.EnableAmbientOcclusion, b => SendMessage(MessageFactory.AmbientOcclusionEnable(b)));
            AmbientOcclusionIntensity = new RProperty<int>(s.AmbientOcclusionIntensity, i => SendMessage(MessageFactory.AmbientOcclusionIntensity(i)));
            Action sendAmbientOcclusionColor = () =>
                SendMessage(MessageFactory.AmbientOcclusionColor(AmbientOcclusionR?.Value ?? 0, AmbientOcclusionG?.Value ?? 0, AmbientOcclusionB?.Value ?? 0));
            AmbientOcclusionR = new RProperty<int>(s.AmbientOcclusionR, _ => sendAmbientOcclusionColor());
            AmbientOcclusionG = new RProperty<int>(s.AmbientOcclusionG, _ => sendAmbientOcclusionColor());
            AmbientOcclusionB = new RProperty<int>(s.AmbientOcclusionB, _ => sendAmbientOcclusionColor());
        }

        #region Image Quality

        public RProperty<int> AntiAliasStyle { get; }
        public RProperty<int> TargetFramerateStyle { get; }
        public RProperty<bool> UseFrameReductionEffect { get; }
        public RProperty<bool> DisableHdrAlways { get; set; }

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
        public RProperty<int> ShadowR { get; }
        public RProperty<int> ShadowG { get; }
        public RProperty<int> ShadowB { get; }
        public RProperty<int> ShadowBlur { get; }
        public RProperty<int> ShadowIntensity { get; }
        public RProperty<int> ShadowYaw { get; }
        public RProperty<int> ShadowPitch { get; }
        public RProperty<int> ShadowDepthOffset { get; }

        public RProperty<bool> EnableFixedShadowAlways { get; }
        public RProperty<bool> EnableFixedShadowWhenLocomotionActive { get; }

        #endregion

        #region Ambient Occlusion

        public RProperty<bool> EnableAmbientOcclusion { get; }
        public RProperty<int> AmbientOcclusionIntensity { get; }
        public RProperty<int> AmbientOcclusionR { get; }
        public RProperty<int> AmbientOcclusionG { get; }
        public RProperty<int> AmbientOcclusionB { get; }

        #endregion

        #region Bloom

        public RProperty<bool> EnableBloom { get; }
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

        #region Rim

        public RProperty<bool> RimEnabled { get; set; }
        public RProperty<int> RimIntensity { get; set; }
        public RProperty<int> RimThickness { get; set; }
        public RProperty<int> RimAngle { get; set; }

        public RProperty<int> RimR { get; set; }
        public RProperty<int> RimG { get; set; }
        public RProperty<int> RimB { get; set; }
        public RProperty<int> RimHdrColorIntensity { get; set; }

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
            var setting = LightSetting.Default;
            TargetFramerateStyle.Value = setting.TargetFramerateStyle;
            UseFrameReductionEffect.Value = setting.UseFrameReductionEffect;
            DisableHdrAlways.Value = setting.DisableHdrAlways;
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
            ShadowR.Value = setting.ShadowR;
            ShadowG.Value = setting.ShadowG;
            ShadowB.Value = setting.ShadowB;
            ShadowBlur.Value = setting.ShadowBlur;
            ShadowIntensity.Value = setting.ShadowIntensity;
            ShadowYaw.Value = setting.ShadowYaw;
            ShadowPitch.Value = setting.ShadowPitch;
            ShadowDepthOffset.Value = setting.ShadowDepthOffset;

            EnableFixedShadowAlways.Value = setting.EnableFixedShadowAlways;
            EnableFixedShadowWhenLocomotionActive.Value = setting.EnableFixedShadowWhenLocomotionActive;
        }

        public void ResetAmbientOcclusionSetting()
        {
            var setting = LightSetting.Default;
            EnableAmbientOcclusion.Value = setting.EnableAmbientOcclusion;
            AmbientOcclusionIntensity.Value = setting.AmbientOcclusionIntensity;
            AmbientOcclusionR.Value = setting.AmbientOcclusionR;
            AmbientOcclusionG.Value = setting.AmbientOcclusionG;
            AmbientOcclusionB.Value = setting.AmbientOcclusionB;
        }

        public void ResetBloomSetting()
        {
            var setting = LightSetting.Default;
            EnableBloom.Value = setting.EnableBloom;
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

        public void ResetRimSetting()
        {
            var setting = LightSetting.Default;
            RimEnabled.Value = setting.RimEnabled;
            RimIntensity.Value = setting.RimIntensity;
            RimThickness.Value = setting.RimThickness;
            RimAngle.Value = setting.RimAngle;
            RimR.Value = setting.RimR;
            RimG.Value = setting.RimG;
            RimB.Value = setting.RimB;
            RimHdrColorIntensity.Value = setting.RimHdrColorIntensity;
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
            ResetAmbientOcclusionSetting();
            ResetBloomSetting();
            ResetOutlineEffectSetting();
            ResetRimSetting();
            ResetWindSetting();
            ResetImageQuality();
        }

        #endregion

    }
}
