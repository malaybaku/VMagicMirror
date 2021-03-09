namespace Baku.VMagicMirror
{
    public class VRMPreviewLanguage
    {
        public const string Japanese = nameof(Japanese);
        public const string English = nameof(English);
        
        public string Language { get; private set; } = Japanese;

        public VRMPreviewLanguage(IMessageReceiver receiver)
        {
            receiver.AssignCommandHandler(
                VmmCommands.Language,
                message => 
                    //NOTE: WPF側が第3言語に対応する可能性があるが、Unity側は日英のみで通す
                    Language = message.Content == "Japanese" ? "Japanese" : "English"
            );
        }

    }
}
