using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    public class ExternalTrackerViewModel : SettingViewModelBase
    {
        //private readonly ExternalTrackerBlendShapeNameStore _blendShapeNameStore
        //    = new ExternalTrackerBlendShapeNameStore();

        private readonly ExternalTrackerSettingModel _model;
        private readonly ExternalTrackerRuntimeConfig _runtimeConfig;
        private readonly MotionSettingModel _motionModel;

        public ExternalTrackerViewModel() : this(
            ModelResolver.Instance.Resolve<ExternalTrackerSettingModel>(),
            ModelResolver.Instance.Resolve<ExternalTrackerRuntimeConfig>(),
            ModelResolver.Instance.Resolve<MotionSettingModel>(),
            ModelResolver.Instance.Resolve<AccessorySettingModel>()
            )
        {
        }


        internal ExternalTrackerViewModel(
            ExternalTrackerSettingModel model,
            ExternalTrackerRuntimeConfig runtimeConfig,
            MotionSettingModel motionModel,
            AccessorySettingModel accessoryModel
            )
        {
            _model = model;
            _runtimeConfig = runtimeConfig;
            _motionModel = motionModel;

            AvailableAccessoryNames = new AccessoryItemNamesViewModel(accessoryModel);

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

            if (IsInDesignMode)
            {
                return;
            }

            UpdateTrackSourceType();
            model.TrackSourceType.AddWeakEventHandler(UpdateTrackSourceTypeAsHandler);

            WeakEventManager<ExternalTrackerSettingModel, EventArgs>.AddHandler(
                model, nameof(model.FaceSwitchSettingReloaded), OnFaceSwitchSettingReloaded);
            WeakEventManager<ExternalTrackerSettingModel, EventArgs>.AddHandler(model, nameof(model.Loaded), OnModelLoaded);
            LoadFaceSwitchSetting();
        }

        private void OnFaceSwitchSettingReloaded(object? sender, EventArgs e)
        {
            if (!_model.IsLoading)
            {
                LoadFaceSwitchSetting();
            }
        }

        private void OnModelLoaded(object? sender, EventArgs e) => LoadFaceSwitchSetting();

        /// <summary>
        /// Face Switchの設定が更新されたときにViewModelに情報を反映します。
        /// 設定ファイルをロードしたときや、設定をリセットしたときに呼び出される想定です。
        /// </summary>
        internal void LoadFaceSwitchSetting()
        {
            //NOTE: 先に名前を更新することで「ComboBoxに無い値をSelectedValueにしちゃう」みたいな不整合を防ぐのが狙い
            _runtimeConfig.RefreshBlendShapeNames();

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

        public RProperty<bool> ShouldNotifyMissingBlendShapeClipNames
            => _runtimeConfig.ShouldNotifyMissingBlendShapeClipNames;
        public RProperty<string> MissingBlendShapeNames
            => _runtimeConfig.MissingBlendShapeNames;

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

        private void UpdateTrackSourceTypeAsHandler(object? sender, PropertyChangedEventArgs e) => UpdateTrackSourceType();
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
            => UrlNavigate.Open(LocalizedString.GetString("URL_docs_ex_tracker"));

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
        public ReadOnlyObservableCollection<string> BlendShapeNames => _runtimeConfig.BlendShapeNames;

        /// <summary> Face Switchで連動させるアクセサリ名の選択肢の一覧です。 </summary>
        public AccessoryItemNamesViewModel AvailableAccessoryNames { get; }

        /// <summary>
        /// 個別のFace Switchで使っているブレンドシェイプ名が変わったとき呼び出すことで、
        /// 使用中のブレンドシェイプ名の情報を実態に合わせます。
        /// </summary>
        public void RefreshUsedBlendshapeNames() => _runtimeConfig.RefreshBlendShapeNames();

        #endregion

        #region エラーまわり: iFMの設定が怪しそうなときのメッセージ + webカメラが止まる問題の対処

        public RProperty<string> IFacialMocapTroubleMessage => _runtimeConfig.IFacialMocapTroubleMessage;

        public ActionCommand OpenIFMTroubleShootCommand { get; }

        private void OpenIFMTroubleShoot() 
            => UrlNavigate.Open(LocalizedString.GetString("URL_ifacialmocap_troubleshoot"));

        public ActionCommand EndExTrackerIfNeededCommand { get; }      

        #endregion
    }
}
