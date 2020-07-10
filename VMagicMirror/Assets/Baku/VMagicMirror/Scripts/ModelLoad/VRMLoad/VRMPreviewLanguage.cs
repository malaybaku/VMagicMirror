namespace Baku.VMagicMirror
{
    public class VRMPreviewLanguage
    {
        public string Language { get; private set; } = "Japanese";

        public VRMPreviewLanguage(IMessageReceiver receiver)
        {
            receiver.AssignCommandHandler(
                VmmCommands.Language,
                message => Language = message.Content
            );
        }
    }
}
