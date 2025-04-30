using System.Threading.Tasks;

namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// IPC用のメッセージを送信する方式を定義します。
    /// </summary>
    /// <remarks>
    /// GRPCにべったり依存するのも嫌なので、簡単なものは文字列送る原始的な方法で済ませる。データ構造が面倒なものは特別に作る。
    /// </remarks>
    internal interface IMessageSender
    {
        /// <summary>
        /// メッセージを送信します。
        /// </summary>
        /// <param name="message"></param>
        void SendMessage(Message message);

        /// <summary>
        /// 返信を取得するメッセージを送信し、結果を取得します。
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task<string> QueryMessageAsync(Message message);
    }
}
