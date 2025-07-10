using Baku.VMagicMirrorConfig.Model;

namespace Baku.VMagicMirrorConfig
{
    //NOTE: このクラスで、メインウィンドウやライセンスに表示する名称を管理します。
    public static class AppConsts
    {
        public static string AppName => "VMagicMirror " + AppVersion.ToString();
        public static string EditionName => FeatureLocker.FeatureLocked ? "Standard Edition" : "Full Edition";
        public static string AppFullName => AppName + " " + EditionName;

        // NOTE: この文字列がアプリのMainWindowに表示される。
        // 開発中のスクリーンショット等でウィンドウ名を変えておいたほうが説明上よい場合、ここをハードコーディングすることで上書きできる。
        public static string AppFullNameWithEnvSuffix => 
            AppFullName + (TargetEnvironmentChecker.CheckDevEnvFlagEnabled() ? "(Dev)" : "");

        public static VmmAppVersion AppVersion => new(4, 1, 0);
    }
}
