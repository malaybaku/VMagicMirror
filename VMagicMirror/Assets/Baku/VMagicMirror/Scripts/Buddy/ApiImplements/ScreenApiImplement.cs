using R3;

namespace Baku.VMagicMirror.Buddy
{
    public class ScreenApiImplement
    {
        private readonly IMessageReceiver _messageReceiver;

        private readonly ReactiveProperty<bool> _windowFrameVisible = new(true);

        public ScreenApiImplement(IMessageReceiver receiver)
        {
            receiver.BindBoolProperty(
                VmmCommands.WindowFrameVisibility,
                _windowFrameVisible
            );
        }
        
        public bool IsTransparent => !_windowFrameVisible.Value;
    }
}
