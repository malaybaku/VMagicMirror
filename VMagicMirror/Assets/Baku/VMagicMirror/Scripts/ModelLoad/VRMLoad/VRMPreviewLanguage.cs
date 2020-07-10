namespace Baku.VMagicMirror
{
    public class VRMPreviewLanguage
    {
        public string Language { get; private set; } = "Japanese";

        public VRMPreviewLanguage(IMessageReceiver receiver)
        {
            receiver.AssignCommandHandler(
                MessageCommandNames.Language,
                message => Language = message.Content
            );
        }
    }
}
