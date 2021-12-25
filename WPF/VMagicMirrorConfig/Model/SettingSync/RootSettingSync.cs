using System.Collections.ObjectModel;
using System.Globalization;

namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// ファイルに保存すべき設定のモデル層を直接的に全部保持したクラス。
    /// MainWindowの裏にあり、アプリの生存期間中つねに単一のインスタンスがあるような使い方をします。
    /// </summary>
    class RootSettingSync
    {
        public RootSettingSync(IMessageSender sender, IMessageReceiver receiver)
        {
            AvailableLanguageNames = new ReadOnlyObservableCollection<string>(_availableLanguageNames);

            _sender = sender;

            Window = new WindowSettingSync(sender);
            Motion = new MotionSettingSync(sender);
            Layout = new LayoutSettingSync(sender);
            Gamepad = new GamepadSettingSync(sender);
            Light = new LightSettingSync(sender);
            WordToMotion = new WordToMotionSettingSync(sender, receiver);
            ExternalTracker = new ExternalTrackerSettingSync(sender);
            Automation = new AutomationSettingSync(sender);
            Accessory = new AccessorySettingSync(sender, receiver);

            //NOTE; LanguageSelectorとの二重管理っぽくて若干アレだがこのままで行く
            //初期値Defaultを入れることで、起動直後にPCのカルチャベースで言語を指定しなきゃダメかどうか判別する
            LanguageName = new RProperty<string>("Default", s =>
            {
                LanguageSelector.Instance.LanguageName = s;
            });
        }

        private readonly IMessageSender _sender;

        private readonly ObservableCollection<string> _availableLanguageNames
            = new ObservableCollection<string>()
        {
            LanguageSelector.LangNameJapanese,
            LanguageSelector.LangNameEnglish,
        };
        public ReadOnlyObservableCollection<string> AvailableLanguageNames { get; }

        //NOTE: 自動ロードがオフなのにロードしたVRMのファイルパスが残ったりするのはメモリ上ではOK。
        //SettingFileIoがセーブする時点において、自動ロードが無効だとファイルパスが転写されないようにガードがかかる。
        public string LastVrmLoadFilePath { get; set; } = "";
        public string LastLoadedVRoidModelId { get; set; } = "";
        //NOTE: このモデル名は保存したデータの内訳を示すために用いる。
        //そのため、主にファイルセーブ + セーブしたファイルのプレビュー目的で使い、ロードでは読み込まない
        public string LoadedModelName { get; set; } = "";

        public RProperty<bool> AutoLoadLastLoadedVrm { get; } = new RProperty<bool>(false);

        //NOTE: VRMのロード処理はUI依存の処理が多すぎるためViewModel実装のままにしている

        //NOTE: この辺3つはオートセーブ以外からは絶対に読み込まないやつ
        public RProperty<string> LanguageName { get; }
        public RProperty<bool> LoadCharacterWhenLoadInternalFile { get; } = new RProperty<bool>(true);
        public RProperty<bool> LoadNonCharacterWhenLoadInternalFile { get; } = new RProperty<bool>(false);

        public WindowSettingSync Window { get; }

        public MotionSettingSync Motion { get; }

        public LayoutSettingSync Layout { get; }

        public GamepadSettingSync Gamepad { get; }

        public LightSettingSync Light { get; }

        public WordToMotionSettingSync WordToMotion { get; }

        public ExternalTrackerSettingSync ExternalTracker { get; }

        public AutomationSettingSync Automation { get; }
        public AccessorySettingSync Accessory { get; }

        public void InitializeAvailableLanguage(string[] languageNames)
        {
            foreach (var name in languageNames)
            {
                _availableLanguageNames.Add(name);
            }
        }

        /// <summary>
        /// 自動保存される設定ファイルに言語設定が保存されていなかった場合、
        /// 現在のカルチャに応じた初期言語を設定します。
        /// </summary>
        public void InitializeLanguageIfNeeded()
        {
            if (LanguageName.Value == "Default")
            {
                LanguageName.Value =
                    (CultureInfo.CurrentCulture.Name == "ja-JP") ?
                    LanguageSelector.LangNameJapanese :
                    LanguageSelector.LangNameEnglish;
            }
        }

        public void OnVRoidModelLoaded(string modelId)
        {
            LastVrmLoadFilePath = "";
            LastLoadedVRoidModelId = modelId;
        }

        public void OnLocalModelLoaded(string filePath)
        {
            LastVrmLoadFilePath = filePath;
            LastLoadedVRoidModelId = "";
        }

        /// <summary>
        /// アプリケーションが起動されたまま、全ての設定を初期状態にします。
        /// </summary>
        /// <remarks>
        /// 理論上はコレを使えば再起動無しでリセットできるんだけど、実際うまくいってないケースがありそう…。
        /// より安全な方法として、現在は_autosaveファイル自体を削除してアプリを落とす手段を提供している。
        /// 消すのは勿体ないため、呼び出し元はないけどこのメソッドは残します。
        /// </remarks>
        public void ResetToDefault()
        {
            _sender.StartCommandComposite();

            AutoLoadLastLoadedVrm.Value = false;
            LastVrmLoadFilePath = "";
            LastLoadedVRoidModelId = "";

            Window.ResetToDefault();
            Motion.ResetToDefault();
            Layout.ResetToDefault();
            Gamepad.ResetToDefault();
            Light.ResetToDefault();
            WordToMotion.ResetToDefault();
            ExternalTracker.ResetToDefault();
            Accessory.ResetToDefault();

            _sender.EndCommandComposite();
        }
    }
}
