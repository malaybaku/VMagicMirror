using System;
using System.Runtime.CompilerServices;

namespace Baku.VMagicMirror
{
    class MessageFactory
    {
        //Singleton
        private static MessageFactory _instance;
        public static MessageFactory Instance
            => _instance ?? (_instance = new MessageFactory());
        private MessageFactory() { }

        //コマンド名 = メソッド名になる(NoArgもWithArgも共通)
        private static Message NoArg([CallerMemberName]string command = "")
            => new Message(command);

        private static Message WithArg(string content, [CallerMemberName]string command = "")
            => new Message(command, content);

        public Message CloseConfigWindow() => NoArg();

        public Message SetCalibrateFaceData(string data) => WithArg(data);

        public Message SetBlendShapeNames(string v) => WithArg(v);
    }
}