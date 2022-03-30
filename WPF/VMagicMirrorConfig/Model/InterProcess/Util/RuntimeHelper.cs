namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// モデルのうち初期化と終了処理をまとめておくと都合が良さそうなものを集めているクラス。
    /// 細分化したクラスに分け直してもよい
    /// </summary>
    class RuntimeHelper
    {
        public RuntimeHelper() : this(
            ModelResolver.Instance.Resolve<IMessageSender>(),
            ModelResolver.Instance.Resolve<IMessageReceiver>(),
            ModelResolver.Instance.Resolve<RootSettingModel>()
            )
        {
        }

        public RuntimeHelper(IMessageSender sender, IMessageReceiver receiver, RootSettingModel mainModel)
        {
            _receiver = receiver;
            MouseButtonMessageSender = new MouseButtonMessageSender(sender);
            CameraPositionChecker = new CameraPositionChecker(sender, mainModel.Layout);
            UnityAppCloser = new UnityAppCloser(receiver);
            ErrorIndicator = new ErrorMessageIndicator(receiver);
            FreeLayoutHelper = new DeviceFreeLayoutHelper(mainModel.Layout, mainModel.Window);
        }

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
    }
}
