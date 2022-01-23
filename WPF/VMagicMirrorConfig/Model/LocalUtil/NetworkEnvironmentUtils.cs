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
        private static readonly UdpClient _udpClientIpv6 = new UdpClient(AddressFamily.InterNetworkV6);

        /// <summary>
        /// IPv4のローカルアドレスっぽいものを取得します。
        /// </summary>
        /// <returns></returns>
        public static string GetLocalIpv4AddressAsString() => GetLocalAddressString(AddressFamily.InterNetwork);

        /// <summary>
        /// IPv6のローカルアドレスっぽいものを取得します。
        /// </summary>
        /// <returns></returns>
        public static string GetLocalIpv6AddressAsString() => GetLocalAddressString(AddressFamily.InterNetworkV6);

        private static string GetLocalAddressString(AddressFamily family)
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            return host.AddressList
                .Where(ip => ip.AddressFamily == family)
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
            switch(address.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    _udpClient.Send(data, data.Length, new IPEndPoint(address, 49983));
                    break;
                case AddressFamily.InterNetworkV6:
                    _udpClientIpv6.Send(data, data.Length, new IPEndPoint(address, 49983));
                    break;
                default:
                    var indication = MessageIndication.InvalidIpAddress();
                    await MessageBoxWrapper.Instance.ShowAsync(indication.Title, indication.Content, MessageBoxWrapper.MessageBoxStyle.OK);
                    break;
            }

        }
    }
}
