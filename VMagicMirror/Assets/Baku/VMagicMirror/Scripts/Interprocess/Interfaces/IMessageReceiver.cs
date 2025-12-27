using System;
using R3;
using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary> IPCによるメッセージを受け取ってリダイレクトする処理を定義します。 </summary>
    public interface IMessageReceiver
    {
        /// <summary>
        /// コマンド名とハンドラを指定してコマンドを受信できるようにします。指定したハンドラはメインスレッドで呼ばれます。
        /// </summary>
        /// <param name="command"></param>
        /// <param name="handler"></param>
        void AssignCommandHandler(VmmCommands command, Action<ReceivedCommand> handler);

        /// <summary>
        /// クエリ名をハンドラを指定してクエリを受信できるようにします。指定したハンドラはメインスレッドで呼ばれます。
        /// </summary>
        /// <param name="query"></param>
        /// <param name="handler"></param>
        void AssignQueryHandler(VmmCommands query, Action<ReceivedQuery> handler);

        //NOTE: 現状Unregister的なものは不要。繋ぎっぱなしでよいため。
    }

    /// <summary> Unity内部の処理で、あたかも外部からメッセージが来たように捌きたいときに使えるインターフェース </summary>
    public interface IMessageDispatcher
    {
        /// <summary>
        /// アプリ外部から受け取ったという扱いでメッセージを流します
        /// </summary>
        /// <param name="command"></param>
        void ReceiveCommand(ReceivedCommand command);
    }

    //TODO: AssignCommandHandlerの直呼びを極力減らして下記に寄せていく
    // Assign~ が引き続き使われる想定ケースはJSONが飛んできてパースするやつとか
    public static class MessageReceiverExtension
    {
        public static void BindAction(this IMessageReceiver receiver, VmmCommands command, Action action)
        {
            receiver.AssignCommandHandler(command, _ => action());
        }
        
        public static void BindBoolProperty(
            this IMessageReceiver receiver,
            VmmCommands command,
            ReactiveProperty<bool> target)
        {
            receiver.AssignCommandHandler(command, c => target.Value = c.ToBoolean());
        }

        public static void BindIntProperty(
            this IMessageReceiver receiver,
            VmmCommands command,
            ReactiveProperty<int> target)
        {
            receiver.AssignCommandHandler(command, c => target.Value = c.ToInt());
        }
        
        public static void BindEnumProperty<T>(
            this IMessageReceiver receiver,
            VmmCommands command,
            ReactiveProperty<T> target) where T : Enum
        {
            receiver.AssignCommandHandler(command, 
                c => target.Value = (T)Enum.ToObject(typeof(T), c.ToInt())
                );
        }
        
        public static void BindPercentageProperty(
            this IMessageReceiver receiver,
            VmmCommands command,
            ReactiveProperty<float> target)
        {
            receiver.AssignCommandHandler(command, c => target.Value = c.ParseAsPercentage());
        }

        public static void BindCentimeterProperty(
            this IMessageReceiver receiver,
            VmmCommands command,
            ReactiveProperty<float> target)
        {
            receiver.AssignCommandHandler(command, c => target.Value = c.ParseAsCentimeter());
        }

        public static void BindColorProperty(
            this IMessageReceiver receiver,
            VmmCommands command,
            ReactiveProperty<Color> target)
        {
            receiver.AssignCommandHandler(command, c =>
            {
                var argb = c.ToColorFloats();
                target.Value = argb.Length == 4
                    ? new Color(argb[0], argb[1], argb[2], argb[3])
                    : new Color(argb[0], argb[1], argb[2]);
            });
        }

        public static void BindStringProperty(
            this IMessageReceiver receiver,
            VmmCommands command,
            ReactiveProperty<string> target
        )
        {
            receiver.AssignCommandHandler(command, c => target.Value = c.GetStringValue());
        }
    }
}
