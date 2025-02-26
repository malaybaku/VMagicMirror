using UniRx;

namespace Baku.VMagicMirror
{
    public class BuddySettingsRepository : PresenterBase
    {
        private readonly IMessageReceiver _receiver;
        public BuddySettingsRepository(IMessageReceiver receiver)
        {
            _receiver = receiver;
        }

        public override void Initialize()
        {
            _receiver.BindBoolProperty(
                VmmCommands.BuddySetMainAvatarOutputActive,
                _mainAvatarOutputActive
                );
        }

        private readonly ReactiveProperty<bool> _mainAvatarOutputActive = new(false);
        // TODO: この設定を使うクラスを用意して、そのクラスからのイベントをいい感じに遮断したりする 
        public IReadOnlyReactiveProperty<bool> MainAvatarOutputActive => _mainAvatarOutputActive;
    }
}
