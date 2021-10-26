using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace Baku.VMagicMirrorConfig
{
    static class CommandArrayBuilder
    {
        public static string BuildCommandArrayString(IEnumerable<Message> commands)
        {
            //キーをC(Command)とA(Args)にするのは文字列長を節約するため
            return new JObject()
            {
                ["items"] = new JArray(commands.Select(c => new JObject()
                {
                    ["C"] = c.Command,
                    ["A"] = c.Content
                }))
            }.ToString(Formatting.None);
        }
    }
}
