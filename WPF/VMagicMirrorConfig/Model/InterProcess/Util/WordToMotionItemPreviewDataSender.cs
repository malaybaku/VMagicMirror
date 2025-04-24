using System;
using System.Threading;
using System.Threading.Tasks;

namespace Baku.VMagicMirrorConfig
{
    class WordToMotionItemPreviewDataSender
    {
        public WordToMotionItemPreviewDataSender(IMessageSender sender)
        {
            _sender = sender;
            MotionRequest = MotionRequest.GetDefault();
        }

        /// <summary>送信機能が有効な間、このリクエストの中身を送信します。</summary>
        public MotionRequest MotionRequest { get; }

        //あくまでプレビューが目当てなのでザツに。
        private const int DataSendIntervalMillisec = 500;
        private readonly IMessageSender _sender;
        private CancellationTokenSource? _cts = new CancellationTokenSource();

        public event EventHandler? PrepareDataSend;

        public void Start()
        {
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    PrepareDataSend?.Invoke(this, EventArgs.Empty);
                    SendData(MotionRequest);
                    await Task.Delay(DataSendIntervalMillisec, _cts.Token);
                }
            });
        }

        public void End()
        {
            _cts?.Cancel();
            _cts = null;
        }

        private void SendData(MotionRequest request)
        {
            string content = request.ToVrm10Json();
            _sender.SendMessage(
                MessageFactory.SendWordToMotionPreviewInfo(content)
                );
        }
    }
}
