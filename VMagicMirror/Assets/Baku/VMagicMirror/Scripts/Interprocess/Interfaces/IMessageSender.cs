using System;
using System.Threading.Tasks;

namespace Baku.VMagicMirror
{
    public interface IMessageSender
    {
        /// <summary>
        /// NOTE: 第2引数をtrueにしていいのはアプリから送信する最後のコマンドのみ。
        /// </summary>
        /// <param name="message"></param>
        /// <param name="isLastMessage"></param>
        void SendCommand(Message message, bool isLastMessage = false);
        Task<string> SendQueryAsync(Message message);

        event Action<Message> SendingMessage;

        /// <summary>
        /// <see cref="SendCommand"/> の第2引数が true であるような呼び出しを行ったあと、そのコマンドの書き込みが完了すると true になる。
        /// そもそも第2引数が true の呼び出しがない場合は false のままになる
        /// </summary>
        bool LastMessageSent { get; }
    }
}
