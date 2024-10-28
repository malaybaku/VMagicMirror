using Baku.VMagicMirrorConfig.Model;

namespace Baku.VMagicMirrorConfig
{
    //NOTE: このクラスで、メインウィンドウやライセンスに表示する名称を管理します。
    public static class AppConsts
    {
        public static string AppName => "VMagicMirror " + AppVersion.ToString();
        public static string EditionName => FeatureLocker.FeatureLocked ? "Standard Edition" : "Full Edition";
        public static string AppFullName => AppName + " " + EditionName;
        public static string AppFullNameWithEnvSuffix => 
            AppFullName + (TargetEnvironmentChecker.CheckDevEnvFlagEnabled() ? "(Dev)" : "");

        public static VmmAppVersion AppVersion => new VmmAppVersion(3, 8, 4);
    }
}
