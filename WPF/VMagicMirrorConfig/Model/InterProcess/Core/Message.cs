namespace Baku.VMagicMirrorConfig
{
    class Message
    {
        //NOTE: コマンドにはコロン(":")を入れない事！(例外スローの方が健全かも)
        public Message(string command, string content)
        {
            Command = command?.Replace(":", "") ?? "";
            Content = content ?? "";
        }

        //パラメータが無いものはコレで十分
        public Message(string command) : this(command, "")
        {
        }

        public string Command { get; }
        public string Content { get; }
    }
}
