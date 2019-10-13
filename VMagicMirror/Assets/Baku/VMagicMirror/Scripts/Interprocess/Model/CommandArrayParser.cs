using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public static class CommandArrayParser
    {
        public static IEnumerable<ReceivedCommand> ParseCommandArray(string content)
        {
            try
            {
                return JsonUtility.FromJson<MessageItemArray>(content)
                    .items
                    .Select(c => new ReceivedCommand(c.C, c.A));
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
                return Enumerable.Empty<ReceivedCommand>();
            }
        }
    }
    
    [Serializable]
    public class MessageItem
    {
        public string C;
        public string A;
    }
    
    [Serializable]
    public class MessageItemArray
    {
        public MessageItem[] items;
    }
}
