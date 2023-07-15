namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// セーブデータ
    /// </summary>
    /// <remarks>
    /// リフレクションベースでシリアライズしている関係 + 後方互換性の関係で
    /// - クラス名はSaveDataでなければならない
    /// - このクラスのプロパティ名は安易に変えてはいけない
    /// という制限があります。(LightSettingに実際はエフェクトの設定が入ってるのもそれのせいです)
    /// </remarks>
    public class SaveData
    {
        public bool IsInternalSaveFile { get; set; } = false;

        public string? LastLoadedVrmFilePath { get; set; } = "";

        public string? LastLoadedVRoidModelId { get; set; } = "";

        //NOTE: ファイルでもVRoid Hubでもモデルの表示名を引っ張ってきて代入するような場所。
        //Unityのほうで、モデルをロードしたときに表示名を投げつけてくれる。
        public string? LastLoadedVrmName { get; set; } = "";

        public bool AutoLoadLastLoadedVrm { get; set; } = false;

        public string? PreferredLanguageName { get; set; } = "";

        public bool LoadCharacterWhenLoad { get; set; } = true;
        public bool LoadNonCharacterWhenLoad { get; set; } = false;

        public WindowSetting? WindowSetting { get; set; }

        public MotionSetting? MotionSetting { get; set; }

        public LayoutSetting? LayoutSetting { get; set; }

        public LightSetting? LightSetting { get; set; }

        public WordToMotionSetting? WordToMotionSetting { get; set; }
        public VMCPSetting? VMCPSetting { get; set; }

        public ExternalTrackerSetting? ExternalTrackerSetting { get; set; }
        public AccessorySetting? AccessorySetting { get; set; }

        public AutomationSetting? AutomationSetting { get; set; }
    }

    //NOTE: メタデータ的にモデル名だけ読み込むときのシリアライズで使うクラスです
    public class SaveDataOnlyModelName
    {
        public string? LastLoadedVrmName { get; set; } = "";
    }
}
