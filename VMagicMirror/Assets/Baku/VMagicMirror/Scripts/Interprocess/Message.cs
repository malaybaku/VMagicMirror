namespace Baku.VMagicMirror
{
    public class Message
    {
        public Message(string command, string content)
        {
            Command = command?.Replace(":", "") ?? "";
            Content = content ?? "";
        }

        public Message(string command) : this(command, "")
        {
        }

        public string Command { get; }
        public string Content { get; }
    }
}
