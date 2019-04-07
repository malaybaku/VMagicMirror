using System.Linq;

namespace Baku.VMagicMirror
{
    public struct ReceivedQuery
    {
        public ReceivedQuery(string command) : this(command, "")
        {
        }

        public ReceivedQuery(string command, string content)
        {
            Command = command ?? "";
            Content = content ?? "";
            Result = "";
        }

        public string Command { get; }
        public string Content { get; }
        public string Result { get; set; }

        public bool ToBoolean()
            => bool.TryParse(Content, out bool result) ?
            result :
            false;

        public int ToInt()
            => int.TryParse(Content, out int result) ? result : 0;

        public float ParseAsPercentage()
            => int.TryParse(Content, out int result) ?
                result * 0.01f :
                0.0f;

        public float ParseAsCentimeter()
            => int.TryParse(Content, out int result) ?
                result * 0.01f :
                0.0f;

        public int[] ToIntArray()
            => Content.Split(',')
                .Select(e => int.TryParse(e, out int result) ? result : 0)
                .ToArray();

        /// <summary>
        /// コマンドによってLength==3(RGB)の場合とLength==4(ARGB)の場合があるので注意
        /// </summary>
        /// <returns></returns>
        public float[] ToColorFloats()
            => ToIntArray()
            .Select(v => v / 255.0f)
            .ToArray();
    }
}
