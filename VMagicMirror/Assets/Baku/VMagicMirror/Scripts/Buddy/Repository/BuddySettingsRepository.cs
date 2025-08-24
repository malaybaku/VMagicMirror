using Baku.VMagicMirror.Buddy;
using R3;

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
                VmmCommands.BuddySetInteractionApiEnabled,
                _interactionApiEnabled
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

        // NOTE: 初期値はEditionで変化する。ちょっと特殊
        // (※「常にGUI側が値を明示する」ということにして常にfalse初期値にしたほうが良いかも)
        private readonly ReactiveProperty<bool> _interactionApiEnabled = new(
            !FeatureLocker.IsFeatureLocked
            );
        public ReadOnlyReactiveProperty<bool> InteractionApiEnabled => _interactionApiEnabled;

        private readonly ReactiveProperty<bool> _developerModeActive = new(false);
        public ReadOnlyReactiveProperty<bool> DeveloperModeActive => _developerModeActive;

        private readonly ReactiveProperty<int> _developerModeLogLevel = new();

        private readonly ReactiveProperty<BuddyLogLevel> _logLevel = new();
        public ReadOnlyReactiveProperty<BuddyLogLevel> LogLevel => _logLevel;
    }
}
