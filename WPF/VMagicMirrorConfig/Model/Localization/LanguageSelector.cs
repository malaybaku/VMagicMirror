using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace Baku.VMagicMirrorConfig
{
    class LanguageSelector : NotifiableBase
    {
        //NOTE: 外部ファイルで他の名前も指定できることに注意
        public const string LangNameJapanese = "Japanese";
        public const string LangNameEnglish = "English";

        private static LanguageSelector? _instance;
        public static LanguageSelector Instance => _instance ??= new LanguageSelector();
        private LanguageSelector() 
        {
            AvailableLanguageNames = new ReadOnlyObservableCollection<string>(_availableLanguageNames);
        }

        private IMessageSender? _sender = null;
        private readonly LocalizationDictionaryLoader _dictionaryLoader = new LocalizationDictionaryLoader();

        /// <summary>
        /// <see cref="LanguageName"/>が変化すると発火します。
        /// </summary>
        /// <remarks>
        /// 切り替わったあとの言語名は現状では不要。LocalizedStringで文字列取得する分には関知不要なため。
        /// </remarks>
        public event Action? LanguageChanged;

        private readonly ObservableCollection<string> _availableLanguageNames = new ObservableCollection<string>()
        {
            LangNameJapanese, 
            LangNameEnglish,
        };
        public ReadOnlyObservableCollection<string> AvailableLanguageNames { get; }

        private string _languageName = nameof(LangNameJapanese);
        public string LanguageName
        {
            get => _languageName;
            set
            {
                if (_languageName != value && IsValidLanguageName(value))
                {
                    _languageName = value;
                    SetLanguage(LanguageName);
                    LanguageChanged?.Invoke();
                    //NOTE: Bindingしたい人向け
                    RaisePropertyChanged();
                }
            }
        }

        public void Initialize(IMessageSender sender)
        {
            _sender = sender;
            _dictionaryLoader.LoadFromDefaultLocation();
            ApplyFallbackLocalize();

            foreach (var langName in _dictionaryLoader
                .GetLoadedDictionaries()
                .Keys
                //NOTE: 順番が不変じゃなくなってると嫌なので、名前順で不変にしておく
                .OrderBy(v => v)
                .ToArray()
                )
            {
                _availableLanguageNames.Add(langName);
            }
        }

        private bool IsValidLanguageName(string languageName)
        {
            return
                languageName == LangNameJapanese ||
                languageName == LangNameEnglish ||
                _dictionaryLoader.GetLoadedDictionaries().ContainsKey(languageName);
        }

        private void SetLanguage(string languageName)
        {
            //NOTE: 日本語と英語についてはexe内部から読み込む。わざわざ外に配置して壊すのも嫌なので
            var dict =
                (languageName == LangNameJapanese || languageName == LangNameEnglish)
                ? new ResourceDictionary()
                {
                    Source = new Uri(
                        $"/VMagicMirrorConfig;component/Resources/{languageName}.xaml",
                        UriKind.Relative
                        ),
                }
                : _dictionaryLoader.GetLoadedDictionaries()[languageName];

            Application.Current.Resources.MergedDictionaries[0] = dict;
            _sender?.SendMessage(MessageFactory.Language(languageName));
        }

        /// <summary>
        /// 日英以外のローカライズファイルでキーが漏れている場合に英語で補填する処理
        /// </summary>
        private void ApplyFallbackLocalize()
        {
            var englishDict = new ResourceDictionary()
            {
                Source = new Uri($"/VMagicMirrorConfig;component/Resources/English.xaml", UriKind.Relative),
            };
            var englishDictKeys = englishDict.Keys.Cast<string>().ToHashSet();

            foreach (var pair in _dictionaryLoader.GetLoadedDictionaries())
            {
                bool missingKeyFound = false;
                var d = pair.Value;
                var targetKeys = d.Keys.Cast<string>().ToHashSet();
                //NOTE: このExceptが重い気がするが、リソースディクショナリの仕様上こうするしかなさそうなので頑張らないでおく
                foreach (var fallbackKey in englishDictKeys.Except(targetKeys))
                {
                    d[fallbackKey] = englishDict[fallbackKey];

                    //全部指摘してもくどいので、各言語についてローカライズ漏れは最大1箇所だけの指摘に留める
                    if (!missingKeyFound)
                    {
                        missingKeyFound = true;
                        LogOutput.Instance.Write($"Localization '{pair.Key}' does not have key '{fallbackKey}'.");
                    }
                }
            }
        }
    }
}
