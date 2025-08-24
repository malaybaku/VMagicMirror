using R3;
using Zenject;

namespace Baku.VMagicMirror
{
    public class LanguageSettingRepository : PresenterBase
    {
        public const string LanguageNameJapanese = "Japanese";
        public const string LanguageNameEnglish = "English";
        
        private readonly IMessageReceiver _receiver;

        [Inject]
        public LanguageSettingRepository(IMessageReceiver receiver)
        {
            _receiver = receiver;
        }
        
        // NOTE: サブキャラのScriptからも取得することがあることには注意
        // NOTE: マルチスレッドが気になってきたら Atomic<string> にしてもよい
        private readonly ReactiveProperty<string> _language = new("English");
        public string LanguageName => _language.Value;
        
        public override void Initialize()
        {
            _receiver.BindStringProperty(VmmCommands.Language, _language);
        }
    }
}
