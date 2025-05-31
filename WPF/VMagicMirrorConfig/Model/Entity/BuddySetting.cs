namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// サブキャラ関連のログの詳細度。int値として設定ファイルに保存されるので、数値の互換性に注意
    /// </summary>
    public enum BuddyLogLevel
    {
        /// <summary> スクリプトのコンパイルエラーやハンドルされていない重大なエラーのみを出力 </summary>
        Fatal = 0,
        /// <summary> <see cref="None"/>までの内容 + スクリプトでエラー扱いしたログを出力 </summary>
        Error = 1,
        /// <summary> <see cref="Error"/>までの内容 + スクリプトで警告扱いしたログを出力 </summary>
        Warning = 2,
        /// <summary> <see cref="Warning"/>までの内容 + スクリプトでただの情報扱いしたログを出力 </summary>
        Info = 3,
        /// <summary> <see cref="Info"/>までの内容 + システム由来の処理(画像やVRM読み込み等)を出力 </summary>
        Verbose = 4,
    }

    public static class BuddyLogLevelExtension
    {
        public static bool IsMoreSevereThan(this BuddyLogLevel level, BuddyLogLevel other)
            => level < other;

        public static bool IsEqualOrMoreSevereThan(this BuddyLogLevel level, BuddyLogLevel other)
            => level <= other;
    }

    public class BuddySetting : SettingEntityBase
    {
        // NOTE: Standard Editionでは InteractionApi == true のときに視覚エフェクトがかかる仕様がある。
        // この制限がデフォルトでかかっていると邪魔になってしまうので、デフォルトでは無効にしている
        public bool InteractionApiEnabled { get; set; } = FeatureLocker.FeatureLocked ? false : true;
        public bool SyncShadowToMainAvatar { get; set; } = true;
        public bool DeveloperModeActive { get; set; } = false;
        public int DeveloperModeLogLevel { get; set; } = (int)BuddyLogLevel.Fatal;

        public static BuddySetting Default { get; } = new BuddySetting();
    }
}
