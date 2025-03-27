using Baku.VMagicMirror.Buddy;
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
            
            _receiver.BindBoolProperty(
                VmmCommands.BuddySetDeveloperModeActive,
                _developerModeActive
                );
            
            _receiver.BindIntProperty(
                VmmCommands.BuddySetDeveloperModeLogLevel,
                _developerModeLogLevel
                );

            _developerModeActive.CombineLatest(
                _developerModeLogLevel,
                (isDeveloperMode, logLevel) =>
                    isDeveloperMode && logLevel >= (int)BuddyLogLevel.Fatal && logLevel <= (int)BuddyLogLevel.Verbose 
                        ? (BuddyLogLevel)logLevel 
                        : BuddyLogLevel.Fatal)
                .Subscribe(level => _logLevel.Value = level)
                .AddTo(this);

        }

        private readonly ReactiveProperty<bool> _mainAvatarOutputActive = new(false);
        // TODO: この設定を使うクラスを用意して、そのクラスからのイベントをいい感じに遮断したりする 
        public IReadOnlyReactiveProperty<bool> MainAvatarOutputActive => _mainAvatarOutputActive;

        private readonly ReactiveProperty<bool> _developerModeActive = new(false);
        public IReadOnlyReactiveProperty<bool> DeveloperModeActive => _developerModeActive;

        private readonly ReactiveProperty<int> _developerModeLogLevel = new();

        private readonly ReactiveProperty<BuddyLogLevel> _logLevel = new();
        public IReadOnlyReactiveProperty<BuddyLogLevel> LogLevel => _logLevel;
    }
}
