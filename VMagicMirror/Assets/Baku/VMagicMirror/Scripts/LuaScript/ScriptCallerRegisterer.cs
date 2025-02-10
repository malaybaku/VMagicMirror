using UniRx;

namespace Baku.VMagicMirror.Buddy
{
    public class ScriptCallerRegisterer : PresenterBase
    {
        private readonly ScriptLoader _scriptLoader;
        private readonly BuddyPropertyRepository _buddyPropertyRepository;

        public ScriptCallerRegisterer(ScriptLoader scriptLoader, BuddyPropertyRepository buddyPropertyRepository)
        {
            _scriptLoader = scriptLoader;
            _buddyPropertyRepository = buddyPropertyRepository;
        }

        public override void Initialize()
        {
            _scriptLoader.ScriptLoading
                .Subscribe(SetupScriptCaller)
                .AddTo(this);
        }

        private void SetupScriptCaller(ScriptCaller scriptCaller)
        {
            var propertyApi = _buddyPropertyRepository.Get(scriptCaller.BuddyId);
            scriptCaller.SetPropertyApi(propertyApi);
        }
    }
}
