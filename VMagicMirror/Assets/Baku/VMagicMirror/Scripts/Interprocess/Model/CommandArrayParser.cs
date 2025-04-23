using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Baku.VMagicMirror
{
    // TODO: メッセージがstringではなくバイナリベースになるので、ここの構造は見直す必要がある
    // そもそもIPCのメッセージがちゃんとキューを積めるようになったらこのクラスが不要になるので、その方向で解決できるのが一番嬉しい…
    public static class CommandArrayParser
    {
        public static IEnumerable<ReceivedCommand> ParseCommandArray(string content)
        {
            try
            {
                return JsonUtility.FromJson<MessageItemArray>(content)
                    .items
                    .Select(c => new ReceivedCommand((VmmCommands)c.C, c.A));
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
        public int C;
        public string A;
    }
    
    //TODO: メッセージ(byte[])をb64エンコードしたstringの配列…ということにしたい
    [Serializable]
    public class MessageItemArray
    {
        public MessageItem[] items;
    }
}
