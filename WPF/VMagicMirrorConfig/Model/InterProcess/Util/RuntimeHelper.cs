namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// モデルっぽいけど置き場所が難しい処理(スクショ撮影とか)を寄せ集めたクラス。
    /// ViewModelがファットになるのを防ぐために処理を集めるのが主目的なため、
    /// このクラスを細分化したクラスに分け直してもよい
    /// </summary>
    class RuntimeHelper
    {
        public RuntimeHelper(IMessageSender sender, IMessageReceiver receiver, RootSettingModel mainModel)
        {
            _sender = sender;
            _receiver = receiver;
            MouseButtonMessageSender = new MouseButtonMessageSender(sender);
            CameraPositionChecker = new CameraPositionChecker(sender, mainModel.Layout);
            UnityAppCloser = new UnityAppCloser(receiver);
            ErrorIndicator = new ErrorMessageIndicator(receiver);
            FreeLayoutHelper = new DeviceFreeLayoutHelper(mainModel.Layout, mainModel.Window);
        }

        private readonly IMessageSender _sender;
        private readonly IMessageReceiver _receiver;

        public MouseButtonMessageSender MouseButtonMessageSender { get; }
        public CameraPositionChecker CameraPositionChecker { get; }
        public UnityAppCloser UnityAppCloser { get; }
        public ErrorMessageIndicator ErrorIndicator { get; }
        public DeviceFreeLayoutHelper FreeLayoutHelper { get; }

        public void Start()
        {
            MouseButtonMessageSender.Start();
            FreeLayoutHelper.StartObserve();
            CameraPositionChecker.Start(2000);
            new AppExitFromUnityMessage().Initialize(_receiver);
        }

        public void Dispose()
        {
            MouseButtonMessageSender.Dispose();
            FreeLayoutHelper.EndObserve();
            CameraPositionChecker.Stop();
            //NOTE: コイツによるプロセス閉じ処理はsender/receiverに依存しないことに注意。
            UnityAppCloser.Close();
        }

        /// <summary> スクリーンショットの撮影をUnity側に要求します。 </summary>
        public void TakeScreenshot() => _sender.SendMessage(MessageFactory.Instance.TakeScreenshot());

        /// <summary> スクリーンショットの保存フォルダを開くようUnity側に要求します。 </summary>
        public void OpenScreenshotSavedFolder() => _sender.SendMessage(MessageFactory.Instance.OpenScreenshotFolder());

    }
}
