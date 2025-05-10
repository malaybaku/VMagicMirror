using Baku.VMagicMirrorConfig.Model;
using System;
using System.Diagnostics;
using System.IO;

namespace Baku.VMagicMirrorConfig
{
    public static class SpecialFilePath
    {
        //拡張子.vmmを付けるのを期待したファイルだが、付けちゃうとユーザーの誤操作で上書きする懸念があるので、つけない。
        public const string AutoSaveSettingFileName = "_autosave";
        private const string LogTextName = "log_config.txt";
        private const string UnityAppFileName = "VMagicMirror.exe";
        private const string SaveSlotFileNamePrefix = "_save";
        private const string UpdateCheckFileName = "UpdateCheck";

        public const string GameInputSettingFileExt = ".vmm_gi";
        public const string BuddyEntryScriptFileName = "main.csx";

        //TODO: 「デバッグ実行時だけRootDirectoryを差し替えたい」という需要が考えられるが、良い手はあるか…？
        private static string RootDirectory { get; }

        public static string SaveFileDir { get; }
        public static string LogFileDir { get; }
        public static string LogFilePath { get; }
        public static string AccessoryFileDir { get; }
        /// <summary>
        /// BuddyDirはユーザー定義サブキャラのディレクトリ。
        /// デフォルト用の <see cref="DefaultBuddyDir"/> もあることに注意
        /// </summary>
        public static string BuddyDir { get; }
        /// <summary>
        /// NOTE: Editor実行の場合、この値は信用できないので代わりに <see cref="GetDefaultBuddyDirByUnityExePath(string)"/> を使う
        /// </summary>
        public static string DefaultBuddyDir { get; }
        public static string UnityAppPath { get; }
        public static string AutoSaveSettingFilePath { get; }
        public static string UpdateCheckFilePath { get; }

        public static string PreferenceFilePath { get; }
        public static string GameInputDefaultFilePath { get; }
        public static string BuddySettingsFilePath { get; }

        /// <summary>
        /// スロット番号を指定して保存ファイル名を指定します。0を指定した場合は特別にオートセーブファイルのパスを返します。
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public static string GetSaveFilePath(int index) => index == 0 
            ? AutoSaveSettingFilePath 
            : Path.Combine(SaveFileDir, SaveSlotFileNamePrefix + index.ToString());

        public static string GetDefaultBuddyDirByStreamingAssetsPath(string streamingAssetsPath)
        {
            return Path.Combine(streamingAssetsPath, "DefaultBuddy");
        }

        static SpecialFilePath()
        {
            //NOTE: 実際はnullになることはない(コーディングエラーでのみ発生する)
            string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
            string exeDir = Path.GetDirectoryName(exePath) ?? "";
            string unityAppDir = Path.GetDirectoryName(exeDir) ?? "";
            UnityAppPath = Path.Combine(unityAppDir, UnityAppFileName);

            var isDebugRun = TargetEnvironmentChecker.CheckIsDebugEnv();
            RootDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                isDebugRun ? "VMagicMirror_Dev_Files" : "VMagicMirror_Files"
                );
            SaveFileDir = Path.Combine(RootDirectory, "Saves");
            LogFileDir = Path.Combine(RootDirectory, "Logs");
            AccessoryFileDir = Path.Combine(RootDirectory, "Accessory");
            BuddyDir = Path.Combine(RootDirectory, "Buddy");
            AutoSaveSettingFilePath = Path.Combine(SaveFileDir, AutoSaveSettingFileName);
            UpdateCheckFilePath = Path.Combine(SaveFileDir, UpdateCheckFileName);
            LogFilePath = Path.Combine(LogFileDir, LogTextName);
            PreferenceFilePath = Path.Combine(SaveFileDir, "_preferences");
            GameInputDefaultFilePath = Path.Combine(SaveFileDir, "_game_input");
            BuddySettingsFilePath = Path.Combine(SaveFileDir, "_buddy");

            // NOTE: デフォルトのBuddyがUnity側でStreamingAssetsとして置いてある…という前提の処理。
            DefaultBuddyDir = Path.Combine(unityAppDir, "VMagicMirror_Data", "StreamingAssets", "DefaultBuddy");

            Directory.CreateDirectory(RootDirectory);
            Directory.CreateDirectory(SaveFileDir);
            Directory.CreateDirectory(LogFileDir);
            Directory.CreateDirectory(AccessoryFileDir);
        }

        /// <summary>
        /// <see cref="GetSettingFilePath"/>のパスに設定ファイルがあるかどうかを確認します。
        /// </summary>
        /// <returns></returns>
        public static bool IsAutoSaveFileExist() => File.Exists(AutoSaveSettingFilePath);

        // NOTE: Unity側でのパス生成方法に揃えている。
        public static string GetBuddyLogFilePath(string buddyId) 
            => Path.Combine(LogFileDir, "Buddy", buddyId + ".txt");

    }
}
