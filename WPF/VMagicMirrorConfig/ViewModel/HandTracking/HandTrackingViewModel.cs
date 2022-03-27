using System.Collections.ObjectModel;
using System.Windows;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    //TODO: モデルに引っ張られてFaceとMotionが同一ViewModelになっちゃってるが、分けるべき
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

            OpenFullEditionDownloadUrlCommand = new ActionCommand(() => UrlNavigate.Open("https://baku-dreameater.booth.pm/items/3064040"));
            OpenHandTrackingPageUrlCommand = new ActionCommand(() => UrlNavigate.Open(LocalizedString.GetString("URL_docs_hand_tracking")));

            if (!IsInDesignMode)
            {
                //NOTE: ここでは表示にのみ影響するメッセージを受け取るため、ViewModelではあるが直接Receiverのイベントを見に行く
                WeakEventManager<IMessageReceiver, CommandReceivedEventArgs>.AddHandler(
                    receiver,
                    nameof(receiver.ReceivedCommand),
                    OnReceivedCommand
                    );
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

        public RProperty<string> CameraDeviceName => _model.CameraDeviceName;
        public ReadOnlyObservableCollection<string> CameraNames => _deviceListSource.CameraNames;
    }
}
