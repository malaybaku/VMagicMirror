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
        /// <see cref="StartCommandComposite"/>が呼び出されていない場合、メッセージを直ちに送信します。
        /// <see cref="StartCommandComposite"/>が呼び出されている場合、
        /// メッセージは<see cref="EndCommandComposite"/>が呼ばれた時点で一括送信されます。
        /// </summary>
        /// <param name="message"></param>
        void SendMessage(Message message);

        /// <summary>
        /// 返信を取得するメッセージを送信し、結果を取得します。
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task<string> QueryMessageAsync(Message message);

        /// <summary>
        /// <see cref="SendMessage(Message)"/>でのメッセージ送信を蓄積するモードに入ります。
        /// 蓄積されたメッセージは<see cref="EndCommandComposite"/>を呼び出すと一括送信されます。
        /// </summary>
        void StartCommandComposite();

        /// <summary>
        /// <see cref="StartCommandComposite"/>を呼び出して以降に、
        /// <see cref="SendMessage(Message)"/>で送信するよう指定されたメッセージをまとめて送信します。
        /// </summary>
        void EndCommandComposite();
    }
}
