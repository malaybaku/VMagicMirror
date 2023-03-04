using System.Collections.ObjectModel;
using System.Globalization;

namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// 設定ファイルに保存すべき情報を全部保持しているクラス。
    /// </summary>
    class RootSettingModel
    {
        public RootSettingModel()
        {
            var resolver = ModelResolver.Instance;
            Window = resolver.Resolve<WindowSettingModel>();
            Motion = resolver.Resolve<MotionSettingModel>();
            Layout = resolver.Resolve<LayoutSettingModel>();
            Gamepad = resolver.Resolve<GamepadSettingModel>();
            Light = resolver.Resolve<LightSettingModel>();
            WordToMotion = resolver.Resolve<WordToMotionSettingModel>();
            ExternalTracker = resolver.Resolve<ExternalTrackerSettingModel>();
            Automation = resolver.Resolve<AutomationSettingModel>();
            Accessory = resolver.Resolve<AccessorySettingModel>();

            //NOTE; LanguageSelectorとの二重管理っぽくて若干アレだがこのままで行く
            //初期値Defaultを入れることで、起動直後にPCのカルチャベースで言語を指定しなきゃダメかどうか判別する
            LanguageName = new RProperty<string>("Default", s =>
            {
                LanguageSelector.Instance.LanguageName = s;
            });
        }

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

        // LanguageSelectorのLanguageNameと異なり、ファイルにセーブする値を持つ。
        // 実際にResourceDictionaryが適用される値ではないのがポイント
        public RProperty<string> LanguageName { get; }
        public RProperty<bool> MinimizeOnLaunch { get; } = new RProperty<bool>(false);
        public RProperty<bool> LoadCharacterWhenLoadInternalFile { get; } = new RProperty<bool>(true);
        public RProperty<bool> LoadNonCharacterWhenLoadInternalFile { get; } = new RProperty<bool>(false);

        public WindowSettingModel Window { get; }
        public MotionSettingModel Motion { get; }
        public LayoutSettingModel Layout { get; }
        public GamepadSettingModel Gamepad { get; }
        public LightSettingModel Light { get; }
        public WordToMotionSettingModel WordToMotion { get; }
        public ExternalTrackerSettingModel ExternalTracker { get; }
        public AutomationSettingModel Automation { get; }
        public AccessorySettingModel Accessory { get; }

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
    }
}
