using System;
using System.Linq;
using UnityEngine;

namespace Baku.VMagicMirror.Buddy
{
    public class BuddyMessageSender
    {
        private readonly IMessageSender _sender;

        public BuddyMessageSender(IMessageSender sender)
        {
            _sender = sender;
        }

        public void NotifyBuddyLogMessage(string buddyId, string message, BuddyLogLevel logLevel)
        {
            var content = JsonUtility.ToJson(new BuddyLogMessage()
            {
                BuddyId = buddyId,
                Message = message,
                LogLevel = (int)logLevel,
            });
            _sender.SendCommand(MessageFactory.Instance.NotifyBuddyLog(content));
        }
    }
    
    [Serializable]
    public class BuddyLogMessage
    {
        public string BuddyId;
        public string Message;
        public int LogLevel;
    }
}
