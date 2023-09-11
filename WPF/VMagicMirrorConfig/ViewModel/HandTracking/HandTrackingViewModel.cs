using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    public class HandTrackingViewModel : SettingViewModelBase
    {
        public HandTrackingViewModel() : this(
            ModelResolver.Instance.Resolve<MotionSettingModel>(),
            ModelResolver.Instance.Resolve<DeviceListSource>(),
            ModelResolver.Instance.Resolve<IMessageReceiver>()
            )
        {
        }

        internal HandTrackingViewModel(
            MotionSettingModel model,
            DeviceListSource deviceListSource,
            IMessageReceiver receiver)
        {
            _model = model;
            _deviceListSource = deviceListSource;

            CameraDeviceName = new RProperty<string>(_model.CameraDeviceName.Value, v =>
            {
                if (!string.IsNullOrEmpty(v))
                {
                    _model.CameraDeviceName.Value = v;
                }
            });

            OpenFullEditionDownloadUrlCommand = new ActionCommand(() => UrlNavigate.Open("https://baku-dreameater.booth.pm/items/3064040"));
            OpenHandTrackingPageUrlCommand = new ActionCommand(() => UrlNavigate.Open(LocalizedString.GetString("URL_docs_hand_tracking")));
            FixBodyMotionStyleCommand = new ActionCommand(FixBodyMotionStyle);

            if (!IsInDesignMode)
            {
                _model.CameraDeviceName.AddWeakEventHandler(OnCameraDeviceNameChanged);
                //NOTE: ここでは表示にのみ影響するメッセージを受け取るため、ViewModelではあるが直接Receiverのイベントを見に行く
                WeakEventManager<IMessageReceiver, CommandReceivedEventArgs>.AddHandler(
                    receiver,
                    nameof(receiver.ReceivedCommand),
                    OnReceivedCommand
                    );
                _model.EnableImageBasedHandTracking.AddWeakEventHandler(BodyMotionStyleIncorrectMaybeChanged);
                _model.EnableNoHandTrackMode.AddWeakEventHandler(BodyMotionStyleIncorrectMaybeChanged);
            }
        }

        private readonly MotionSettingModel _model;
        private readonly DeviceListSource _deviceListSource;

        private void OnReceivedCommand(object? sender, CommandReceivedEventArgs e)
        {
            switch (e.Command)
            {
                case ReceiveMessageNames.SetHandTrackingResult:
                    HandTrackingResult.SetResult(HandTrackingResultBuilder.FromJson(e.Args));
                    break;
                default:
                    break;
            }
        }

        private void OnCameraDeviceNameChanged(object? sender, PropertyChangedEventArgs e)
        {
            CameraDeviceName.Value = _model.CameraDeviceName.Value;
        }

        private void BodyMotionStyleIncorrectMaybeChanged(object? sender, PropertyChangedEventArgs e)
            => UpdateBodyMotionStyleIncorrect();

        private void UpdateBodyMotionStyleIncorrect()
        {
            BodyMotionStyleIncorrectForHandTracking.Value =
                _model.EnableImageBasedHandTracking.Value &&
                _model.EnableNoHandTrackMode.Value;
        }

        private void FixBodyMotionStyle()
        {
            _model.EnableNoHandTrackMode.Value = false;
            _model.EnableGameInputLocomotionMode.Value = false;
            SnackbarWrapper.Enqueue(LocalizedString.GetString("Snackbar_BodyMotionStyle_Set_Default"));
        }

        public RProperty<bool> EnableImageBasedHandTracking => _model.EnableImageBasedHandTracking;
        private readonly RProperty<bool> _alwaysOn = new RProperty<bool>(true);
        public RProperty<bool> ShowEffectDuringHandTracking => FeatureLocker.FeatureLocked
            ? _alwaysOn
            : _model.ShowEffectDuringHandTracking;
        public bool CanChangeEffectDuringHandTracking => !FeatureLocker.FeatureLocked;
        public RProperty<bool> DisableHandTrackingHorizontalFlip => _model.DisableHandTrackingHorizontalFlip;
        public RProperty<bool> EnableSendHandTrackingResult => _model.EnableSendHandTrackingResult;
        public HandTrackingResultViewModel HandTrackingResult { get; } = new HandTrackingResultViewModel();
        public ActionCommand OpenFullEditionDownloadUrlCommand { get; }
        public ActionCommand OpenHandTrackingPageUrlCommand { get; }
        public ActionCommand FixBodyMotionStyleCommand { get; }

        public RProperty<string> CameraDeviceName { get; }
        public ReadOnlyObservableCollection<string> CameraNames => _deviceListSource.CameraNames;

        public RProperty<bool> BodyMotionStyleIncorrectForHandTracking { get; } = new(false);
    }
}
