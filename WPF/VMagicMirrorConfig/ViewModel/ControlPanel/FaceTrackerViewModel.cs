using Baku.VMagicMirrorConfig.View;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    // NOTE: 開発経緯について
    // - もともと FaceTracker ではなくExternalTracker 専用のViewModelがあった
    // - webカメラによる顔トラッキングの機能が増えた過程で、UIが ExternalTracker 専用ではなく、顔トラッキング全般に関するものにシフトした
    public class FaceTrackerViewModel : SettingViewModelBase
    {
        private readonly ExternalTrackerSettingModel _exTrackerModel;
        private readonly ExternalTrackerRuntimeConfig _runtimeConfig;
        private readonly MotionSettingModel _motionModel;
        private readonly DeviceListSource _deviceList;

        public FaceTrackerViewModel() : this(
            ModelResolver.Instance.Resolve<ExternalTrackerSettingModel>(),
            ModelResolver.Instance.Resolve<ExternalTrackerRuntimeConfig>(),
            ModelResolver.Instance.Resolve<MotionSettingModel>(),
            ModelResolver.Instance.Resolve<AccessorySettingModel>(),
            ModelResolver.Instance.Resolve<DeviceListSource>()
            )
        {
        }


        internal FaceTrackerViewModel(
            ExternalTrackerSettingModel exTrackerModel,
            ExternalTrackerRuntimeConfig runtimeConfig,
            MotionSettingModel motionModel,
            AccessorySettingModel accessoryModel,
            DeviceListSource deviceList
            )
        {
            _exTrackerModel = exTrackerModel;
            _runtimeConfig = runtimeConfig;
            _motionModel = motionModel;
            _deviceList = deviceList;

            AvailableAccessoryNames = new AccessoryItemNamesViewModel(accessoryModel);

            RefreshIFacialMocapTargetCommand = new ActionCommand(
                () => NetworkEnvironmentUtils.SendIFacialMocapDataReceiveRequest(IFacialMocapTargetIpAddress.Value)
                );
            OpenInstructionUrlCommand = new ActionCommand(OpenInstructionUrl);
            OpenPerfectSyncTipsUrlCommand = new ActionCommand(OpenPerfectSyncTipsUrl);
            OpenIFMTroubleShootCommand = new ActionCommand(OpenIFMTroubleShoot);
            EndExTrackerIfNeededCommand = new ActionCommand(
                async () => await exTrackerModel.DisableExternalTrackerWithConfirmAsync()
                );
            ShowMissingBlendShapeNotificationCommand = new ActionCommand(ShowMissingBlendShapeNotification);

            if (IsInDesignMode)
            {
                // NOTE: 視認性のためにプレビュー上ではUIがだいたい展開した状態にする。
                UseLiteWebCamera.Value = true;
                UseHighPowerWebCamera.Value = true;
                HighPowerWebCameraAppliedByHandTracking.Value = true;
                return;
            }

            UpdateTrackSourceType();
            exTrackerModel.TrackSourceType.AddWeakEventHandler(UpdateTrackSourceTypeAsHandler);

            WeakEventManager<ExternalTrackerSettingModel, EventArgs>.AddHandler(
                exTrackerModel, nameof(exTrackerModel.FaceSwitchSettingReloaded), OnFaceSwitchSettingReloaded);
            WeakEventManager<ExternalTrackerSettingModel, EventArgs>.AddHandler(exTrackerModel, nameof(exTrackerModel.Loaded), OnModelLoaded);

            _motionModel.EnableWebCamHighPowerMode.AddWeakEventHandler(OnWebCamHighPowerModeChanged);
            _motionModel.EnableImageBasedHandTracking.AddWeakEventHandler(OnHandTrackingEnabledChanged);
            EnableExternalTracking.AddWeakEventHandler(OnEnableExternalTrackingChanged);
            UpdateBaseMode();

            LoadFaceSwitchSetting();
        }

        private void OnFaceSwitchSettingReloaded(object? sender, EventArgs e)
        {
            if (!_exTrackerModel.IsLoading)
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

            foreach (var item in _exTrackerModel.FaceSwitchSetting.Items)
            {
                var vm = new FaceSwitchItemViewModel(this, item);
                vm.SubscribeLanguageSelector();
                FaceSwitchItems.Add(vm);
            }
        }

        private void OnHandTrackingEnabledChanged(object? sender, PropertyChangedEventArgs e) => UpdateBaseMode();

        private void OnWebCamHighPowerModeChanged(object? sender, PropertyChangedEventArgs e) => UpdateBaseMode();

        private void OnEnableExternalTrackingChanged(object? sender, PropertyChangedEventArgs e) => UpdateBaseMode();

        private void UpdateBaseMode()
        {
            // NOTE: どのフラグが参照されるか明記しないとわかりにくいので一旦全て説明変数で受ける。
            // webcamについて、カメラの使用on/off自体は見に行かないことに注意
            var exTrackerEnabled = _exTrackerModel.EnableExternalTracking.Value;
            var webCamHighPowerModeEnabled = _motionModel.EnableWebCamHighPowerMode.Value;
            var handTrackingEnabled = _motionModel.EnableImageBasedHandTracking.Value;



            var preferHighPowerModeInWebCamera = webCamHighPowerModeEnabled || handTrackingEnabled;
            // NOTE: ハンドトラッキングが有効だと高負荷モードになる (MediaPipeが起動するため)
            UseLiteWebCamera.Value = !exTrackerEnabled && !preferHighPowerModeInWebCamera;
            UseHighPowerWebCamera.Value = !exTrackerEnabled && preferHighPowerModeInWebCamera;
            // 「GUI上で直近で選んだのは軽量トラッキングだが、ハンドトラッキングを有効かしたことで高品質モードに遷移している」の条件を入れている。
            // ちょっと複雑なので注意
            HighPowerWebCameraAppliedByHandTracking.Value =
                !exTrackerEnabled && !webCamHighPowerModeEnabled && handTrackingEnabled;

            FaceSwitchSupported.Value = exTrackerEnabled || UseHighPowerWebCamera.Value;
            // NOTE: ExTrackerはModelのフラグを素通ししているのでそのまんまでOK
        }


        #region 基本モードの状態や、状態に応じた機能の利用可否のフラグ群

        // NOTE: 下記の3フラグはどれか一つだけオンになるように制御される。
        // 「ExTrackerが有効なら残り2つはオフ扱い」などの暗黙の優先度仕様を踏まえて値を制御するので、Settingの値をそのまま反映するわけではない
        public RProperty<bool> UseLiteWebCamera { get; } = new RProperty<bool>(false);
        public RProperty<bool> UseHighPowerWebCamera { get; } = new RProperty<bool>(false);
        public RProperty<bool> HandTrackingEnabled => _motionModel.EnableImageBasedHandTracking;

        public RProperty<bool> EnableExternalTracking => _exTrackerModel.EnableExternalTracking;

        // NOTE: 実は使ってないが、Unity側の状態の情報をWPF側にもいちおう書いときたいので残している
        // 「ハンドトラッキングさえ切ったら低負荷モードになるような組み合わせで高負荷モードになってるとき」だけtrueになるフラグ
        public RProperty<bool> HighPowerWebCameraAppliedByHandTracking { get; } = new RProperty<bool>(false);

        public RProperty<bool> FaceSwitchSupported { get; } = new RProperty<bool>(false);
        // NOTE: 制限つきでFace Switchが動くケースは実際にはwebカメラの高負荷モードだけだが、View向けに読み替えを行ってプロパティを公開してる
        public RProperty<bool> FaceSwitchLimited => UseHighPowerWebCamera;

        // 高負荷Webカメラの場合だけ、「Face Switchが使えるけどCheekPuffとかTongueOutは使えない」という制限がかかる。UI上で注意喚起するために使う
        public RProperty<bool> FaceSwitchHasLimitation => UseHighPowerWebCamera;


        private ActionCommand? _selectWebCamLiteCommand;
        private ActionCommand? _selectWebCamHighPowerCommand;
        private ActionCommand? _selectExTrackerCommand;

        public ActionCommand SelectWebCamLiteCommand 
            => _selectWebCamLiteCommand ??= new ActionCommand(SelectWebCamLite);
        public ActionCommand SelectWebCamHighPowerCommand
            => _selectWebCamHighPowerCommand ??= new ActionCommand(SelectWebCamHighPower);
        public ActionCommand SelectExTrackerCommand
            => _selectExTrackerCommand ??= new ActionCommand(SelectExTracker);

        private void SelectWebCamLite()
        {
            _exTrackerModel.EnableExternalTracking.Value = false;
            _motionModel.EnableWebCamHighPowerMode.Value = false;
        }
        private void SelectWebCamHighPower()
        {
            _exTrackerModel.EnableExternalTracking.Value = false;
            _motionModel.EnableWebCamHighPowerMode.Value = true;
        }

        private void SelectExTracker()
        {
            _exTrackerModel.EnableExternalTracking.Value = true;
        }


        // NOTE: パーフェクトシンクのオン/オフはExTrackerとwebcam(高精度)の双方で同じ値を使う。
        public RProperty<bool> EnablePerfectSync => _exTrackerModel.EnableExternalTrackerPerfectSync;

        #endregion

        // NOTE: webカメラ(高精度) と ExTracker で使う
        public RProperty<bool> EnableBodyLeanZ => _motionModel.EnableBodyLeanZ;

        #region web camera

        // NOTE: 歴史的経緯で名前がねじれてるけど意図的です
        public RProperty<bool> EnableWebCamera => _motionModel.EnableFaceTracking;
        public RProperty<string> WebCameraDeviceName => _motionModel.CameraDeviceName;

        public ReadOnlyObservableCollection<string> WebCameraNames => _deviceList.CameraNames;
        public RProperty<bool> EnableWebCameraHighPowerModeLipSync => _motionModel.EnableWebCameraHighPowerModeLipSync;


        private ActionCommand? _calibrateWebCameraCommand;
        public ActionCommand CalibrateWebCameraCommand => _calibrateWebCameraCommand ??= new ActionCommand(CalibrateWebCamera);

        private void CalibrateWebCamera() => _motionModel.RequestCalibrateFace();

        // NOTE: LowPowerモードにはオリジナルの設定がないし、項目数も少ないので、そっち用のリセットコマンドは実装していない
        private ActionCommand? _resetWebCameraHighPowerModeSettingsCommand;
        public ActionCommand ResetWebCameraHighPowerModeSettingsCommand => _resetWebCameraHighPowerModeSettingsCommand ??= new ActionCommand(ResetWebCameraHighPowerModeSettings);
        private void ResetWebCameraHighPowerModeSettings()
        {
            _motionModel.ResetWebCameraHighPowerModeSettings();
            // NOTE: 歴史的経緯により、このフラグはMotionの一部じゃないことになっているのだが、UI上はこの値もリセットされてないと直感に反するのでリセットしておく
            _exTrackerModel.EnableExternalTrackerPerfectSync.Value = ExternalTrackerSetting.Default.EnableExternalTrackerPerfectSync;
        }

        private ActionCommand? _openEyeCalibrationWindowCommand;
        public ActionCommand OpenEyeCalibrationWindowCommand => _openEyeCalibrationWindowCommand ??= new ActionCommand(OpenEyeCalibrationWindow);
        private void OpenEyeCalibrationWindow() => FaceTrackingEyeCalibrationWindow.OpenOrActivateExistingWindow();

        #endregion

        #region Ex.Tracker

        public RProperty<bool> EnableExternalTrackerLipSync => _exTrackerModel.EnableExternalTrackerLipSync;
        //NOTE: ここだけ外部トラッキングではなく、webカメラの顔トラと共通のフラグを触りに行ってることに注意
        public RProperty<bool> DisableFaceTrackingHorizontalFlip => _motionModel.DisableFaceTrackingHorizontalFlip;



        private ActionCommand? _calibrateCommand = null;
        public ActionCommand CalibrateCommand
            => _calibrateCommand ??= new ActionCommand(Calibrate);

        private void Calibrate() => _exTrackerModel.SendCalibrateRequest();

        private ActionCommand? _resetExternalTrackerSettingsCommand;

        public ActionCommand ResetExternalTrackerSettingsCommand
            => _resetExternalTrackerSettingsCommand ??= new ActionCommand(ResetExternalTrackerSettings);

        private void ResetExternalTrackerSettings() 
            => SettingResetUtils.ResetSingleCategoryAsync(_exTrackerModel.ResetSettingExceptFaceSwitch);

        #endregion


        #region パーフェクトシンク関連

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
                    _exTrackerModel.TrackSourceType.Value = ExternalTrackerSetting.TrackSourceNone;
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
                    _exTrackerModel.TrackSourceType.Value = ExternalTrackerSetting.TrackSourceIFacialMocap;
                }
            }
        }

        private void UpdateTrackSourceTypeAsHandler(object? sender, PropertyChangedEventArgs e) => UpdateTrackSourceType();
        private void UpdateTrackSourceType()
        {
            IsTrackSourceNone = _exTrackerModel.TrackSourceType.Value == ExternalTrackerSetting.TrackSourceNone;
            IsTrackSourceIFacialMocap = _exTrackerModel.TrackSourceType.Value == ExternalTrackerSetting.TrackSourceIFacialMocap;
        }

        //NOTE: 上記のbool2つ+UpdateTrackSourceTypeを廃止し、この整数値を読み込んだViewがConverterで頑張るのでもよい。はず
        public RProperty<int> TrackSourceType => _exTrackerModel.TrackSourceType;

        public RProperty<string> IFacialMocapTargetIpAddress => _exTrackerModel.IFacialMocapTargetIpAddress;

        public ActionCommand RefreshIFacialMocapTargetCommand { get; }

        public ActionCommand OpenInstructionUrlCommand { get; }

        private void OpenInstructionUrl() 
            => UrlNavigate.Open(LocalizedString.GetString("URL_docs_ex_tracker"));

        public RProperty<string> CalibrateData => _exTrackerModel.CalibrateData;

        #endregion

        #region 表情スイッチ

        //UI表示の同期のためだけに使う値で、Modelとは関係ない
        public RProperty<bool> ShowAccessoryOption { get; } = new RProperty<bool>(false);

        /// <summary>
        /// 子要素になってる<see cref="FaceSwitchItemViewModel"/>から呼び出すことで、
        /// 現在の設定を保存した状態にします。
        /// </summary>
        public void SaveFaceSwitchSetting() => _exTrackerModel.SaveFaceSwitchSetting();

        /// <summary> UIで個別設定として表示する、表情スイッチの要素です。 </summary>
        public ObservableCollection<FaceSwitchItemViewModel> FaceSwitchItems { get; }
            = new ObservableCollection<FaceSwitchItemViewModel>();

        /// <summary> Face Switch機能で表示可能なブレンドシェイプ名の一覧です。 </summary>
        public ReadOnlyObservableCollection<string> BlendShapeNames => _runtimeConfig.BlendShapeNames;

        /// <summary> Face Switchで連動させるアクセサリ名の選択肢の一覧です。 </summary>
        public AccessoryItemNamesViewModel AvailableAccessoryNames { get; }

        /// <summary>
        /// 個別のFace Switchで使っているブレンドシェイプ名が変わったとき呼び出すことで、
        /// 使用中のブレンドシェイプ名の情報を実態に合わせます。
        /// </summary>
        public void RefreshUsedBlendshapeNames() => _runtimeConfig.RefreshBlendShapeNames();

        private ActionCommand? _resetFaceSwitchSettingCommand;
        public ActionCommand ResetFaceSwitchSettingCommand
            => _resetFaceSwitchSettingCommand ??= new ActionCommand(ResetFaceSwitchSetting);
        private void ResetFaceSwitchSetting() 
            => SettingResetUtils.ResetSingleCategoryAsync(() => _exTrackerModel.ResetFaceSwitchSetting());

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
