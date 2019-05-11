using System;
using System.Runtime.CompilerServices;
using UnityEngine;

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

        public Message AutoAdjustResults(AutoAdjustParameters parameters)
            => WithArg(JsonUtility.ToJson(parameters));

        //NOTE: 冗長なパラメータが入ってるが、冗長な部分はWPF側に捨てさせる(どうせ既定値しか入ってない)
        public Message AutoAdjustEyebrowResults(AutoAdjustParameters parameters)
            => WithArg(JsonUtility.ToJson(parameters));
    }
}