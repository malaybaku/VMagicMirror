using System;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary> そこそこ重大なエラーなり警告で、WPF側で表示してほしい物がある場合にそれを送る機能 </summary>
    public class ErrorIndicateSender
    {
        [Serializable]
        public class ErrorIndicateData
        {
            public string title;
            public string content;
            public int level;
        }
        
        public enum ErrorLevel
        {
            Info = 0,
            Warning = 1,
            Error = 2,
            Fatal = 3,
        }

        [Inject]
        public ErrorIndicateSender(IMessageSender sender)
        {
            _sender = sender;
        }

        private readonly IMessageSender _sender;
        
        public void SendError(string title, string content, ErrorLevel errorLevel)
        {
            var data = JsonUtility.ToJson(new ErrorIndicateData()
            {
                title = title,
                content = content,
                level = (int)errorLevel,
            });
            _sender.SendCommand(MessageFactory.Instance.RequestShowError(data));
        }
    }
}
