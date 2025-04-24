using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Baku.VMagicMirrorConfig
{
    static class CommandArrayBuilder
    {
        // NOTE: 書いてる通りだが、「各メッセージのバイナリをbase64エンコードしたやつの配列をJSON扱いしたやつ」を投げる。
        // IPCが普通にQueueに対応したら、このクラス自体が不要になる
        public static string BuildCommandArrayString(IEnumerable<Message> commands)
        {
            return new JObject()
            {
                ["items"] = new JArray(
                    commands.Select(c => Convert.ToBase64String(c.Data.Span)).ToArray()
                    ),
            }.ToString(Formatting.None);
        }
    }
}
