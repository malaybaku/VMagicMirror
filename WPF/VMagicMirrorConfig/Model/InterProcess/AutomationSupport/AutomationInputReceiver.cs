using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// オートメーション用にUDPのローカルの口を開けてくれるやつ。
    /// </summary>
    /// <remarks>
    /// ややいい加減な実装に収めつつ、メッセージのフォーマットがJSONなら収拾つけやすいだろ…と見込んだ設計をしています。
    /// </remarks>
    internal class AutomationInputReceiver
    {
        private CancellationTokenSource? _cts;
        public bool IsRunning { get; private set; }

        /// <summary>
        /// 設定ファイルのロードをリクエストされたとき発火します。UIスレッド保証されてない事に注意して下さい。
        /// </summary>
        public event Action<LoadSettingFileArgs>? LoadSettingFileRequested;

        /// <summary>
        /// ポート番号を指定して受信を開始します。
        /// </summary>
        /// <param name="portNumber"></param>
        public void Start(int portNumber)
        {
            if (IsRunning)
            {
                return;
            }

            LogOutput.Instance.Write($"Start Automation Receive on port {portNumber}");
            IsRunning = true;
            _cts = new CancellationTokenSource();
            Task.Run(() => ReceiveRoutine(_cts.Token, portNumber));
        }

        /// <summary>
        /// 受信を停止します。アプリの終了前には必ず呼び出して下さい。
        /// </summary>
        public void Stop()
        {
            if (!IsRunning)
            {
                return;
            }

            //NOTE: スレッド側でIsRunningを折らせるとタイミング問題が生じるため、ここで即座に折ってしまう
            LogOutput.Instance.Write("Stopped Automation Receive");
            IsRunning = false;
            _cts?.Cancel();
            _cts = null;
        }

        private void HandleTextMessage(string message)
        {
            //NOTE: この実装時点では設定ファイルのロードにしか対応しないため、原始的にパースする。
            //条件文のところはnullableに配慮してワチャワチャしています(増えてきたら必ずサブルーチン化して下さい)
            var jobj = JObject.Parse(message);
            if (jobj.Property("command")?.Value is JValue commandValue &&
                commandValue.Type == JTokenType.String &&
                commandValue.Value<string>() == "load_setting_file" &&
                jobj["args"] is JObject args
                )
            {
                //NOTE: indexが拾えなくて0になると無効値になるため、ロードは走らない
                int index =
                    (args.Property("index")?.Value is JValue indexValue && indexValue.Type == JTokenType.Integer)
                    ? indexValue.Value<int>()
                    : 0;
                bool loadCharacter =
                    args.Property("load_character")?.Value is JValue v && v.Type == JTokenType.Boolean
                    ? (bool)v : false;
                bool loadNonCharacter =
                    args.Property("load_non_character")?.Value is JValue v2 && v2.Type == JTokenType.Boolean
                    ? (bool)v2 : false;

                LoadSettingFileRequested?.Invoke(new LoadSettingFileArgs(index, loadCharacter, loadNonCharacter));
            }
            else
            {
                LogOutput.Instance.Write("receive json style UDP message, but command format is not correct");
            }
        }

        private void ReceiveRoutine(CancellationToken token, int portNumber)
        {
            var client = new UdpClient(portNumber);
            client.Client.ReceiveTimeout = 500;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    IPEndPoint? remoteEndPoint = null;
                    byte[] data = client.Receive(ref remoteEndPoint);
                    try
                    {
                        string message = Encoding.UTF8.GetString(data);
                        HandleTextMessage(message);
                    }
                    catch (Exception ex)
                    {
                        //文字コードとかフォーマットのエラーのはずなので、ログを出す
                        LogOutput.Instance.Write(ex);
                    }
                }
                catch (Exception)
                {
                    //通信待ちや通信終了による例外スローに関してはログ無し。
                }
            }
            client.Close();
        }
    }

    internal class LoadSettingFileArgs
    {
        public LoadSettingFileArgs(int index, bool loadCharacter, bool loadNonCharacter)
        {
            Index = index;
            LoadCharacter = loadCharacter;
            LoadNonCharacter = loadNonCharacter;               
        }

        public int Index { get; }
        public bool LoadCharacter { get; }
        public bool LoadNonCharacter { get; }
    }
}
