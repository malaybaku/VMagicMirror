using System;

namespace Baku.VMagicMirror.InterProcess
{
    /// <summary> IPCによるメッセージを受け取ってリダイレクトする処理を定義します。 </summary>
    public interface IMessageReceiver
    {
        /// <summary>
        /// コマンド名とハンドラを指定してコマンドを受信できるようにします。指定したハンドラはメインスレッドで呼ばれます。
        /// </summary>
        /// <param name="command"></param>
        /// <param name="handler"></param>
        void AssignCommandHandler(string command, Action<ReceivedCommand> handler);

        /// <summary>
        /// クエリ名をハンドラを指定してクエリを受信できるようにします。指定したハンドラはメインスレッドで呼ばれます。
        /// </summary>
        /// <param name="query"></param>
        /// <param name="handler"></param>
        void AssignQueryHandler(string query, Action<ReceivedQuery> handler);

        //NOTE: 現状Unregister的なものは不要。繋ぎっぱなしでよいため。
    }
}
