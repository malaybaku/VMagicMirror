using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Baku.VMagicMirror.ExternalTracker
{
    /// <summary>
    /// 一般性の高い文字列を特定のホスト/ポート指定で受信するだけの処理
    /// </summary>
    public class UdpFaceTrackDataReceiver : MonoBehaviour
    {
        //[SerializeField] private string host = "";
        [SerializeField] private int port = 56912;
        
        /// <summary>
        /// UDPのデータを受け取るとUIスレッド上で発火します。
        /// 1フレームで複数のメッセージを受け取った場合、最後に受け取ったものだけを発火します。
        /// </summary>
        public event Action<string> ReceiveMessage;

        private CancellationTokenSource _cts = null;
        
        private readonly ConcurrentQueue<string> _messages = new ConcurrentQueue<string>();
        private readonly ConcurrentQueue<string> _errorHistory = new ConcurrentQueue<string>();

        public async void StartReceive() 
        {
             StopReceive();
             LogOutput.Instance.Write("UDP Start Receive");
             
             _cts = new CancellationTokenSource();
             var token = _cts.Token;

             UdpClient client = null;
             try
             {
                 client = new UdpClient(new IPEndPoint(IPAddress.Any, port));
                 {

                     while (!token.IsCancellationRequested)
                     {
                         var res = await client.ReceiveAsync();
                         if (token.IsCancellationRequested)
                         {
                             return;
                         }

                         try
                         {
                             string message = Encoding.UTF8.GetString(res.Buffer);
                             _messages.Enqueue(message);
                         }
                         catch (Exception ex)
                         {
                             //UIスレッドじゃないのでデータ溜めるだけです。
                             _errorHistory.Enqueue("UDP string decode error, " + ex.Message);
                         }
                     }
                 }
             }
             catch (Exception e)
             {
                 //メッセージが文字列じゃなかったケース以上にすごいエラーが起きた場合はここに到達
                 _errorHistory.Enqueue("UDP fatal error, " + e.Message);
             }
             finally
             {
                 client?.Close();
             }
        }

        public void StopReceive()
        {
            LogOutput.Instance.Write("UDP Stop Receive");
            _cts?.Cancel();
            _cts = null;
        }
        
        private void Update()
        {
            if (_errorHistory.TryDequeue(out var exMessage))
            {
                LogOutput.Instance.Write("UDP error, message:" + exMessage);
            }

            //NOTE: 最新値しか使わないならそもそもキューじゃなくてatomic<string>的なやつでもいいかも。
            if (_messages.TryDequeue(out var firstResult))
            {
                string message = firstResult;
                while (_messages.TryDequeue(out var result))
                {
                    message = result;
                }
                ReceiveMessage?.Invoke(message);
            }
        }

        private void OnDestroy()
        {
            StopReceive();
        }
    }
}
