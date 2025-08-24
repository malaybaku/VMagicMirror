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
            HalfFpsMode = new RProperty<bool>(s.HalfFpsMode, v => SendMessage(MessageFactory.SetHalfFpsMode(v)));
            UseFrameReductionEffect = new RProperty<bool>(
                s.UseFrameReductionEffect, v => SendMessage(MessageFactory.UseFrameReductionEffect(v)));

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
            FixedShadowYaw = new RProperty<int>(s.FixedShadowYaw, i => SendMessage(MessageFactory.FixedShadowYaw(i)));
            FixedShadowPitch = new RProperty<int>(s.FixedShadowPitch, i => SendMessage(MessageFactory.FixedShadowPitch(i)));

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

        public RProperty<bool> EnableFixedShadowAlways { get; }
        public RProperty<bool> EnableFixedShadowWhenLocomotionActive { get; }
        public RProperty<int> FixedShadowYaw { get; }
        public RProperty<int> FixedShadowPitch { get; }

        #endregion

        #region Ambient Occlusion

        public RProperty<bool> EnableAmbientOcclusion { get; }
        public RProperty<int> AmbientOcclusionIntensity { get; }
        public RProperty<int> AmbientOcclusionR { get; }
        public RProperty<int> AmbientOcclusionG { get; }
        public RProperty<int> AmbientOcclusionB { get; }

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

            EnableFixedShadowAlways.Value = setting.EnableFixedShadowAlways;
            EnableFixedShadowWhenLocomotionActive.Value = setting.EnableFixedShadowWhenLocomotionActive;
            FixedShadowYaw.Value = setting.FixedShadowYaw;
            FixedShadowPitch.Value = setting.FixedShadowPitch;
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
            ResetAmbientOcclusionSetting();
            ResetBloomSetting();
            ResetOutlineEffectSetting();
            ResetWindSetting();
            ResetImageQuality();
        }

        #endregion

    }
}
