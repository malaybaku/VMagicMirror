using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// 値を書き換えたときに良い感じに通信でき、かつロード/セーブに対応したエフェクト関連のモデル
    /// </summary>
    class LightSettingModel : SettingModelBase<LightSetting>
    {
        public LightSettingModel(IMessageSender sender) : base(sender)
        {
            var s = LightSetting.Default;
            var factory = MessageFactory.Instance;

            //モデルのプロパティ変更=Unityへの変更通知としてバインド。
            //エフェクト関係は設定項目がシンプルなため、例外はほぼ無い(色関係のメッセージ送信がちょっと特殊なくらい)

            ImageQualityNames = new ReadOnlyObservableCollection<string>(_imageQualityNames);
            ImageQuality = new RProperty<string>("", s => SendMessage(factory.SetImageQuality(s)));
            HalfFpsMode = new RProperty<bool>(s.HalfFpsMode, v => SendMessage(factory.SetHalfFpsMode(v)));

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

            EnableWind = new RProperty<bool>(s.EnableWind, b => SendMessage(factory.WindEnable(b)));
            WindStrength = new RProperty<int>(s.WindStrength, i => SendMessage(factory.WindStrength(i)));
            WindInterval = new RProperty<int>(s.WindInterval, i => SendMessage(factory.WindInterval(i)));
            WindYaw = new RProperty<int>(s.WindYaw, i => SendMessage(factory.WindYaw(i)));
        }

        #region Image Quality

        //NOTE: 画質設定はもともとUnityが持っており、かつShift+ダブルクリックの起動によって書き換えられる可能性があるので、
        //WPF側からは揮発性データのように扱う 
        public RProperty<string> ImageQuality { get; }

        private readonly ObservableCollection<string> _imageQualityNames = new ObservableCollection<string>();
        public ReadOnlyObservableCollection<string> ImageQualityNames { get; }
        public RProperty<bool> HalfFpsMode { get; }

        public async Task InitializeQualitySelectionsAsync()
        {
            string res = await SendQueryAsync(MessageFactory.Instance.GetQualitySettingsInfo());
            var info = ImageQualityInfo.ParseFromJson(res);
            if (info.ImageQualityNames != null &&
                info.CurrentQualityIndex >= 0 &&
                info.CurrentQualityIndex < info.ImageQualityNames.Length
                )
            {
                foreach (var name in info.ImageQualityNames)
                {
                    _imageQualityNames.Add(name);
                }
                ImageQuality.Value = info.ImageQualityNames[info.CurrentQualityIndex];
            }
        }

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
        public async Task ResetImageQualityAsync()
        {
            HalfFpsMode.Value = false;
            var qualityName = await SendQueryAsync(MessageFactory.Instance.ApplyDefaultImageQuality());
            if (ImageQualityNames.Contains(qualityName))
            {
                ImageQuality.Value = qualityName;
            }
            else
            {
                LogOutput.Instance.Write($"Invalid image quality `{qualityName}` applied");
            }
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
            ResetWindSetting();

            //NOTE: ここだけ非同期なのが何だかな～という感じなんだけど、実害が無いはずなのでOKとします
            Task.Run(async () => await ResetImageQualityAsync());
        }

        #endregion

    }
}
