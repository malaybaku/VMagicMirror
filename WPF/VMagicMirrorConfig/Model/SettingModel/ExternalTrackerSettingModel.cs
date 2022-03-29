using System;
using System.Net;
using System.Threading.Tasks;

namespace Baku.VMagicMirrorConfig
{
    class ExternalTrackerSettingModel : SettingModelBase<ExternalTrackerSetting>
    {
        public ExternalTrackerSettingModel() : this(ModelResolver.Instance.Resolve<IMessageSender>())
        {
        }

        public ExternalTrackerSettingModel(IMessageSender sender) : base(sender)
        {
            var setting = ExternalTrackerSetting.Default;
            var factory = MessageFactory.Instance;

            //NOTE: ひとまず初期値を入れておくと非null保証できて都合がいい、という話
            _faceSwitchSetting = ExternalTrackerFaceSwitchSetting.LoadDefault();

            EnableExternalTracking = new RProperty<bool>(
                setting.EnableExternalTracking, b => SendMessage(factory.ExTrackerEnable(b))
                );
            EnableExternalTrackerLipSync = new RProperty<bool>(
                setting.EnableExternalTrackerLipSync, b => SendMessage(factory.ExTrackerEnableLipSync(b))
                );
            EnableExternalTrackerEmphasizeExpression = new RProperty<bool>(
                setting.EnableExternalTrackerEmphasizeExpression, b => SendMessage(factory.ExTrackerEnableEmphasizeExpression(b))
                );
            EnableExternalTrackerPerfectSync = new RProperty<bool>(
                setting.EnableExternalTrackerPerfectSync, b => SendMessage(factory.ExTrackerEnablePerfectSync(b))
                );

            TrackSourceType = new RProperty<int>(setting.TrackSourceType, i => SendMessage(factory.ExTrackerSetSource(i)));
            //NOTE: このアドレスはコマンド実行時に使うため、書き換わってもメッセージは送らない
            IFacialMocapTargetIpAddress = new RProperty<string>(setting.IFacialMocapTargetIpAddress);

            CalibrateData = new RProperty<string>(
                setting.CalibrateData, s => SendMessage(factory.ExTrackerSetCalibrateData(s))
                );

            SerializedFaceSwitchSetting = new RProperty<string>(
                setting.SerializedFaceSwitchSetting, v => SendMessage(factory.ExTrackerSetFaceSwitchSetting(v))
                );

            //NOTE: この時点で、とりあえずデフォルト設定がUnityに送られる
            SaveFaceSwitchSetting();
        }

        // 基本メニュー部分
        public RProperty<bool> EnableExternalTracking { get; }
        public RProperty<bool> EnableExternalTrackerLipSync { get; }
        public RProperty<bool> EnableExternalTrackerEmphasizeExpression { get; }

        public RProperty<bool> EnableExternalTrackerPerfectSync { get; }

        // アプリ別設定
        public RProperty<int> TrackSourceType { get; }
        public RProperty<string> IFacialMocapTargetIpAddress { get; }
        public RProperty<string> CalibrateData { get; }

        // FaceSwitchの設定

        private ExternalTrackerFaceSwitchSetting _faceSwitchSetting;
        public ExternalTrackerFaceSwitchSetting FaceSwitchSetting
        {
            get => _faceSwitchSetting;
            private set
            {
                //NOTE: モデル層では設定を細かくいじる事はなくて、必ず設定まるごとガッとリロードする。
                //このガッとリロードしたのをイベント検出できるべき、というモチベから、こう書いている
                if (_faceSwitchSetting != value)
                {
                    _faceSwitchSetting = value;
                    RaisePropertyChanged();
                    FaceSwitchSettingReloaded?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// <see cref="FaceSwitchSetting"/>がファイルロードやリセット処理によってリロードされたとき発火
        /// </summary>
        public event EventHandler? FaceSwitchSettingReloaded;


        //NOTE: コンストラクタが終了した時点でちゃんとしたデータが入った状態になる
        public RProperty<string> SerializedFaceSwitchSetting { get; }

        public void SaveFaceSwitchSetting()
        {
            //文字列で保存 + 送信しつつ、手元の設定もリロードする。イベントハンドリング次第でもっとシンプルになるかも。
            SerializedFaceSwitchSetting.Value = FaceSwitchSetting.ToJson();
        }

        //NOTE: 想定挙動としてはセーブ前の時点で値が更新された時点でシリアライズされているため、
        //この再シリアライズをしたからといって普通は値は変わらない。
        //が、初期値そのままのケースとかが安全になって都合がよい
        protected override void PreSave() => SaveFaceSwitchSetting();

        protected override void AfterLoad(ExternalTrackerSetting entity)
        {
            try
            {
                FaceSwitchSetting =
                    ExternalTrackerFaceSwitchSetting.FromJson(SerializedFaceSwitchSetting.Value);
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
                FaceSwitchSetting = ExternalTrackerFaceSwitchSetting.LoadDefault();
            }
        }

        public override void ResetToDefault()
        {
            Load(ExternalTrackerSetting.Default);

            //NOTE: Entityのデフォルト値ではFaceSwitch設定が空になっているため、明示的にデフォルトを読み直す
            FaceSwitchSetting = ExternalTrackerFaceSwitchSetting.LoadDefault();
            SaveFaceSwitchSetting();
        }

        public void SendCalibrateRequest() => SendMessage(MessageFactory.Instance.ExTrackerCalibrate());

        public async Task DisableExternalTrackerWithConfirmAsync()
        {
            //NOTE: これもモデル層…いやメッセージボックス相当だからVMでいいのかな…？
            var indication = MessageIndication.ExTrackerCheckTurnOff();
            bool result = await MessageBoxWrapper.Instance.ShowAsync(
                indication.Title,
                indication.Content,
                MessageBoxWrapper.MessageBoxStyle.OKCancel
                );

            if (result)
            {
                EnableExternalTracking.Value = false;
            }
        }

        public void RefreshConnectionIfPossible()
        {
            //NOTE: 今は選択肢がiFacialMocapのみのため、そこをピンポイントで見に行く
            if (!EnableExternalTracking.Value || TrackSourceType.Value != ExternalTrackerSetting.TrackSourceIFacialMocap)
            {
                return;
            }

            var ipAddress = IFacialMocapTargetIpAddress.Value;
            if (IPAddress.TryParse(ipAddress, out _))
            {
                NetworkEnvironmentUtils.SendIFacialMocapDataReceiveRequest(ipAddress);
            }
        }
    }
}
