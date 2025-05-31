namespace Baku.VMagicMirrorConfig
{
    class ScreenshotTaker
    {
        public ScreenshotTaker() : this(ModelResolver.Instance.Resolve<IMessageSender>())
        {
        }

        public ScreenshotTaker(IMessageSender sender)
        {
            _sender = sender;
        }
        private IMessageSender _sender;

        /// <summary> スクリーンショットの撮影をUnity側に要求します。 </summary>
        public void TakeScreenshot() => _sender.SendMessage(MessageFactory.TakeScreenshot());

        /// <summary> スクリーンショットの保存フォルダを開くようUnity側に要求します。 </summary>
        public void OpenScreenshotSavedFolder() => _sender.SendMessage(MessageFactory.OpenScreenshotFolder());
    }
}
