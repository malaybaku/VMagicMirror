using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public class UdpReceiver : MonoBehaviour
    {
        [SerializeField]
        private ReceivedMessageHandler handler = null;

        [SerializeField]
        string ipAddress = "127.0.0.1";

        //NOTE: ポート番号に深い意味はない
        [SerializeField]
        int port = 53241;

        [SerializeField]
        int receiveTimeoutMillisec = 2000;

        private CancellationTokenSource _cts;

        private readonly object _receivedMessagesLock = new object();
        private Queue<string> _receivedMessages;
        public Queue<string> ReceivedMessages
        {
            get { lock(_receivedMessagesLock) return _receivedMessages; }
            set { lock (_receivedMessagesLock) _receivedMessages = value; }
        }

        void Start()
        {
            ReceivedMessages = new Queue<string>();
            _cts = new CancellationTokenSource();
            Task.Run(() => ReceiveUdpMessage(_cts.Token));
        }

        void Update()
        {
            if (ReceivedMessages.Count == 0)
            {
                return;
            }

            while (ReceivedMessages.Count > 0)
            {
                handler?.Receive(ReceivedMessages.Dequeue());
            }
        }

        private void OnDestroy() 
            => _cts?.Cancel();

        private void ReceiveUdpMessage(CancellationToken token)
        {
            var endPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
            var client = new UdpClient(endPoint);
            //ポイント: タイムアウトつけてないとThread.Abortできないんですよコレが
            client.Client.ReceiveTimeout = receiveTimeoutMillisec;

            //Origin情報は使う予定ない
            IPEndPoint endPointOrigin = new IPEndPoint(IPAddress.Loopback, 0);
            while (!token.IsCancellationRequested)
            {
                try
                {
                    byte[] received = client.Receive(ref endPointOrigin);
                    ReceivedMessages.Enqueue(Encoding.UTF8.GetString(received));
                }

                catch (SocketException)
                {
                    //何もしない: 単にハング防止してるだけ
                    //Logger.Instance.Info("wait for udp data...");
                }
                catch (TimeoutException)
                {
                    //同上
                }
            }
        }

    }
}

