using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Baku.VMagicMirrorConfig
{
    /// <summary> ネットワーク環境について何か情報提供してくれるユーティリティ </summary>
    static class NetworkEnvironmentUtils
    {
        private static readonly UdpClient _udpClient = new UdpClient();

        /// <summary>
        /// IPv4のローカルアドレスっぽいものを取得します。
        /// </summary>
        /// <returns></returns>
        public static string GetLocalIpAddressAsString()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            //空ではないIPv4アドレスをてきとーに拾うだけ。これで十分じゃない場合は…多分それは人類には難しいやつ…
            return host.AddressList
                .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                .Select(ip => ip.ToString())
                .Where(s => !string.IsNullOrEmpty(s))
                .FirstOrDefault()
                ?? "(unknown)";
        }

        /// <summary>
        /// iFacialMocapのデータ受信をiOS機器に対してリクエストします。
        /// 内部的には決め打ちのUDPメッセージを一発打つだけです。
        /// </summary>
        /// <param name="ipAddress">IPv4でiOS機器の端末を指定したLAN内のIPアドレス。</param>
        public static async void SendIFacialMocapDataReceiveRequest(string ipAddress)
        {
            if (!IPAddress.TryParse(ipAddress, out var address))
            {
                var indication = MessageIndication.InvalidIpAddress();
                await MessageBoxWrapper.Instance.ShowAsync(indication.Title, indication.Content, MessageBoxWrapper.MessageBoxStyle.OK);
                return;
            }

            var data = Encoding.UTF8.GetBytes("iFacialMocap_sahuasouryya9218sauhuiayeta91555dy3719");
            _udpClient.Send(data, data.Length, new IPEndPoint(address, 49983));
        }
    }
}
