using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    public class ExternalTrackerViewModel : SettingViewModelBase
    {
        private readonly ExternalTrackerBlendShapeNameStore _blendShapeNameStore
            = new ExternalTrackerBlendShapeNameStore();

        private readonly ExternalTrackerSettingModel _model;
        private readonly MotionSettingModel _motionModel;

        public ExternalTrackerViewModel() : this(
            ModelResolver.Instance.Resolve<ExternalTrackerSettingModel>(),
            ModelResolver.Instance.Resolve<MotionSettingModel>(),
            ModelResolver.Instance.Resolve<AccessorySettingModel>(),
            ModelResolver.Instance.Resolve<IMessageReceiver>()
            )
        {
        }


        internal ExternalTrackerViewModel(
            ExternalTrackerSettingModel model,
            MotionSettingModel motionModel,
            AccessorySettingModel accessoryModel,
            IMessageReceiver receiver
            )
        {
            _model = model;
            _motionModel = motionModel;

            AvailableAccessoryNames = new AccessoryItemNamesViewModel(accessoryModel);

            //この辺はModel/VMの接続とかコマンド周りの設定
            UpdateTrackSourceType();
            model.TrackSourceType.PropertyChanged += (_, __) => UpdateTrackSourceType();
            model.EnableExternalTracking.PropertyChanged += (_, __) => UpdateShouldNotifyMissingBlendShapeClipNames();

            MissingBlendShapeNames = new RProperty<string>(
                "", _ => UpdateShouldNotifyMissingBlendShapeClipNames()
                );

            model.FaceSwitchSettingReloaded += (_, __) =>
            {
                if (!model.IsLoading)
                {
                    LoadFaceSwitchSetting();
                }
            };
            model.Loaded += (_, __) => LoadFaceSwitchSetting();

            RefreshIFacialMocapTargetCommand = new ActionCommand(
                () => NetworkEnvironmentUtils.SendIFacialMocapDataReceiveRequest(IFacialMocapTargetIpAddress.Value)
                );
            OpenInstructionUrlCommand = new ActionCommand(OpenInstructionUrl);
            OpenPerfectSyncTipsUrlCommand = new ActionCommand(OpenPerfectSyncTipsUrl);
            OpenIFMTroubleShootCommand = new ActionCommand(OpenIFMTroubleShoot);
            EndExTrackerIfNeededCommand = new ActionCommand(
                async () => await model.DisableExternalTrackerWithConfirmAsync()
                );
            ShowMissingBlendShapeNotificationCommand = new ActionCommand(ShowMissingBlendShapeNotification);
            ResetSettingsCommand = new ActionCommand(
                () => SettingResetUtils.ResetSingleCategoryAsync(_model.ResetToDefault)
                );

            //TODO: メッセージ受信の処理もモデル側案件のはず…うーん…
            receiver.ReceivedCommand += OnMessageReceived;

            LoadFaceSwitchSetting();
        }

        /// <summary>
        /// Face Switchの設定が更新されたときにViewModelに情報を反映します。
        /// 設定ファイルをロードしたときや、設定をリセットしたときに呼び出される想定です。
        /// </summary>
        internal void LoadFaceSwitchSetting()
        {
            //NOTE: 先に名前を更新することで「ComboBoxに無い値をSelectedValueにしちゃう」みたいな不整合を防ぐのが狙い
            _blendShapeNameStore.Refresh(_model.FaceSwitchSetting);

            foreach (var item in FaceSwitchItems)
            {
                item.UnsubscribeLanguageSelector();
            }
            FaceSwitchItems.Clear();

            foreach (var item in _model.FaceSwitchSetting.Items)
            {
                var vm = new ExternalTrackerFaceSwitchItemViewModel(this, item);
                vm.SubscribeLanguageSelector();
                FaceSwitchItems.Add(vm);
            }
        }

        private void OnMessageReceived(object? sender, CommandReceivedEventArgs e)
        {
            if (e.Command == ReceiveMessageNames.ExtraBlendShapeClipNames)
            {
                try
                {
                    //いちおう信頼はするけどIPCだし…みたいな書き方です
                    var names = e.Args
                        .Split(',')
                        .Where(w => !string.IsNullOrEmpty(w))
                        .ToArray();
                    _blendShapeNameStore.Refresh(names);
                }
                catch (Exception ex)
                {
                    LogOutput.Instance.Write(ex);
                }
            }
            else if (e.Command == ReceiveMessageNames.ExTrackerCalibrateComplete)
            {
                //キャリブレーション結果を向こうから受け取る: この場合は、ただ覚えてるだけでよい
                _model.CalibrateData.SilentSet(e.Args);
            }
            else if (e.Command == ReceiveMessageNames.ExTrackerSetPerfectSyncMissedClipNames)
            {
                MissingBlendShapeNames.Value = e.Args;
            }
            else if (e.Command == ReceiveMessageNames.ExTrackerSetIFacialMocapTroubleMessage)
            {
                IFacialMocapTroubleMessage.Value = e.Args;
            }
        }

        #region 基本メニュー部分

        public RProperty<bool> EnableExternalTracking => _model.EnableExternalTracking;
        public RProperty<bool> EnableExternalTrackerLipSync => _model.EnableExternalTrackerLipSync;
        public RProperty<bool> EnableExternalTrackerEmphasizeExpression => _model.EnableExternalTrackerEmphasizeExpression;
        //NOTE: ここだけ外部トラッキングではなく、webカメラの顔トラと共通のフラグを触りに行ってることに注意
        public RProperty<bool> DisableFaceTrackingHorizontalFlip => _motionModel.DisableFaceTrackingHorizontalFlip;
        public RProperty<bool> EnableExternalTrackerPerfectSync => _model.EnableExternalTrackerPerfectSync;

        public ActionCommand OpenPerfectSyncTipsUrlCommand { get; }

        private void OpenPerfectSyncTipsUrl()
        {
            string url = LocalizedString.GetString("URL_tips_perfect_sync");
            UrlNavigate.Open(url);
        }

        public RProperty<bool> ShouldNotifyMissingBlendShapeClipNames { get; } = new RProperty<bool>(false);

        public RProperty<string> MissingBlendShapeNames { get; }

        private void UpdateShouldNotifyMissingBlendShapeClipNames()
        {
            ShouldNotifyMissingBlendShapeClipNames.Value =
                EnableExternalTracking.Value &&
                !string.IsNullOrEmpty(MissingBlendShapeNames.Value);
        }

        public ActionCommand ShowMissingBlendShapeNotificationCommand { get; }
        private async void ShowMissingBlendShapeNotification()
        {
            var indication = MessageIndication.ExTrackerMissingBlendShapeNames();
            var lines = MissingBlendShapeNames.Value.Split('\n').ToList();
            if (lines.Count > 8)
            {
                //未定義ブレンドシェイプがあまりに多いとき、後ろを"…"で切る
                lines = lines.Take(8).ToList();
                lines.Add("…");
            }

            await MessageBoxWrapper.Instance.ShowAsync(
                indication.Title,
                indication.Content + string.Join("\n", lines),
                MessageBoxWrapper.MessageBoxStyle.OK
                );
        }

        private ActionCommand? _calibrateCommand = null;
        public ActionCommand CalibrateCommand
            => _calibrateCommand ??= new ActionCommand(Calibrate);

        private void Calibrate() => _model.SendCalibrateRequest();
        public ActionCommand ResetSettingsCommand { get; }

        #endregion

        #region アプリ別のやつ(※今んとこIPを一方的に表示するだけなのであんまり難しい事はないです)

        private bool _isTrackSourceNone;
        public bool IsTrackSourceNone
        {
            get => _isTrackSourceNone;
            set
            {
                if (SetValue(ref _isTrackSourceNone, value) && value)
                {
                    _model.TrackSourceType.Value = ExternalTrackerSetting.TrackSourceNone;
                }
            }
        }

        private bool _isTrackSourceIFacialMocap;
        public bool IsTrackSourceIFacialMocap
        {
            get => _isTrackSourceIFacialMocap;
            set
            {
                if (SetValue(ref _isTrackSourceIFacialMocap, value) && value)
                {
                    _model.TrackSourceType.Value = ExternalTrackerSetting.TrackSourceIFacialMocap;
                }
            }
        }

        private void UpdateTrackSourceType()
        {
            IsTrackSourceNone = _model.TrackSourceType.Value == ExternalTrackerSetting.TrackSourceNone;
            IsTrackSourceIFacialMocap = _model.TrackSourceType.Value == ExternalTrackerSetting.TrackSourceIFacialMocap;
        }

        //NOTE: 上記のbool2つ+UpdateTrackSourceTypeを廃止し、この整数値を読み込んだViewがConverterで頑張るのでもよい。はず
        public RProperty<int> TrackSourceType => _model.TrackSourceType;

        public RProperty<string> IFacialMocapTargetIpAddress => _model.IFacialMocapTargetIpAddress;

        public ActionCommand RefreshIFacialMocapTargetCommand { get; }

        public ActionCommand OpenInstructionUrlCommand { get; }

        private void OpenInstructionUrl()
        {
            string url = LocalizedString.GetString("URL_docs_ex_tracker");
            UrlNavigate.Open(url);
        }

        public RProperty<string> CalibrateData => _model.CalibrateData;

        #endregion

        #region 表情スイッチのやつ

        //UI表示の同期のためだけに使う値で、Modelとは関係ない
        public RProperty<bool> ShowAccessoryOption { get; } = new RProperty<bool>(false);

        /// <summary>
        /// 子要素になってる<see cref="ExternalTrackerFaceSwitchItemViewModel"/>から呼び出すことで、
        /// 現在の設定を保存した状態にします。
        /// </summary>
        public void SaveFaceSwitchSetting() => _model.SaveFaceSwitchSetting();

        /// <summary> UIで個別設定として表示する、表情スイッチの要素です。 </summary>
        public ObservableCollection<ExternalTrackerFaceSwitchItemViewModel> FaceSwitchItems { get; }
            = new ObservableCollection<ExternalTrackerFaceSwitchItemViewModel>();

        /// <summary> Face Switch機能で表示可能なブレンドシェイプ名の一覧です。 </summary>
        public ReadOnlyObservableCollection<string> BlendShapeNames => _blendShapeNameStore.BlendShapeNames;

        /// <summary> Face Switchで連動させるアクセサリ名の選択肢の一覧です。 </summary>
        public AccessoryItemNamesViewModel AvailableAccessoryNames { get; }

        /// <summary>
        /// 個別のFace Switchで使っているブレンドシェイプ名が変わったとき呼び出すことで、
        /// 使用中のブレンドシェイプ名の情報を実態に合わせます。
        /// </summary>
        internal void RefreshUsedBlendshapeNames() => _blendShapeNameStore.Refresh(_model.FaceSwitchSetting);

        #endregion

        #region エラーまわり: iFMの設定が怪しそうなときのメッセージ + webカメラが止まる問題の対処

        public RProperty<string> IFacialMocapTroubleMessage { get; } = new RProperty<string>("");

        public ActionCommand OpenIFMTroubleShootCommand { get; }

        private void OpenIFMTroubleShoot()
        {
            var url = LocalizedString.GetString("URL_ifacialmocap_troubleshoot");
            UrlNavigate.Open(url);
        }

        public ActionCommand EndExTrackerIfNeededCommand { get; }

        public void RefreshConnectionIfPossible()
        {
            //NOTE: 今は選択肢がiFacialMocapのみのため、そこをピンポイントで見に行く
            if (!EnableExternalTracking.Value || !IsTrackSourceIFacialMocap)
            {
                return;
            }

            var ipAddress = IFacialMocapTargetIpAddress.Value;
            if (IPAddress.TryParse(ipAddress, out _))
            {
                NetworkEnvironmentUtils.SendIFacialMocapDataReceiveRequest(ipAddress);
            }
        }

        #endregion
    }
}
