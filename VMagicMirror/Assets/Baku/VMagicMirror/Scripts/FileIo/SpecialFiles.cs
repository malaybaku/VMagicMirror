using System;
using System.IO;
using Baku.VMagicMirror.Buddy;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public static class SpecialFiles
    {
        private static readonly string AutoSaveSettingFilePath1;
        
        //セーブファイルには拡張子をつけない(手で触らない想定なため)
        private const string AutoSaveSettingFileName = "_autosave";
        private const string LogTextName = "log.txt";

        public const string BuddyEntryScriptFileName = "main.csx";
        
        public static bool UseDevFolder
        {
            get
            {
#if DEV_ENV
                //dev専用ビルドのフラグを立ててるときだけココを通過する
                return true;
#endif
                return Application.isEditor;
            }
        }
        
        private static string RootDirectory { get; }
        private static string SaveFileDir { get; }
        public static string LogFileDir { get; }
        public static string LogFilePath { get; }
        public static string BuddyLogFileDir { get; }
        public static string DefaultBuddyLogFileDir { get; }

        public static string AutoSaveSettingFilePath { get; }

        public static bool AutoSaveSettingFileExists() => File.Exists(AutoSaveSettingFilePath);
        
        public static string ScreenShotDirectory { get; }

        public static string DefaultBuddyRootDirectory { get; }
        public static string BuddyRootDirectory { get; }

        //モーションやテクスチャ差し替えは以下の優先度になることに注意
        //- エディタの場合: StreamingAssets
        //- dev実行: VMM_Dev_Files
        //- prod実行: VMM_Files
        public static string MotionsDirectory => Application.isEditor 
            ? Path.Combine(Application.streamingAssetsPath, "Motions") 
            : Path.Combine(RootDirectory, "Motions");

        public static string LoopMotionsDirectory
            => Path.Combine(MotionsDirectory, "Loop");

        public static string GetTextureReplacementPath(string textureFileName) => Application.isEditor
            ? Path.Combine(Application.streamingAssetsPath, textureFileName)
            : Path.Combine(RootDirectory, "Textures", textureFileName);
        
        //アクセサリはWPF/Unity双方からディレクトリ走査する都合上、エディタ実行であってもstreamingAssetsは使わない
        public static string AccessoryDirectory => Path.Combine(RootDirectory, "Accessory");

        // NOTE: 下記の2つはデフォルトサブキャラ用の処理は不要 (単にユーザーがスクリプト補完するためのものなので)
        public static string BuddyReferenceDataDirectory 
            => Path.Combine(BuddyRootDirectory, "_Reference");
        public static string BuddyReferenceDataGlobalScriptPath
            => Path.Combine(BuddyReferenceDataDirectory, "Globals.csx");

        // NOTE: .txt にするのはメインのログファイルと揃えつつ、
        // WPFからファイルを開くときにテキストエディタで開かれるのを保証しやすくするため
        public static string GetBuddyLogFilePath(BuddyFolder folder)
        {
            if (folder.IsDefaultBuddy)
            {
                return Path.Combine(DefaultBuddyLogFileDir, folder.FolderName + ".txt");
            }
            else
            {
                return Path.Combine(BuddyLogFileDir, folder.FolderName + ".txt");
            }
        }
        
        static SpecialFiles()
        {
            RootDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
                UseDevFolder ? "VMagicMirror_Dev_Files" : "VMagicMirror_Files"
                );

            ScreenShotDirectory = Path.Combine(RootDirectory, "Screenshots");
            SaveFileDir = Path.Combine(RootDirectory, "Saves");
            LogFileDir = Path.Combine(RootDirectory, "Logs");
            BuddyRootDirectory = Path.Combine(RootDirectory, "Buddy");
            DefaultBuddyRootDirectory = Path.Combine(
                Application.streamingAssetsPath, StreamingAssetFileNames.DefaultBuddyFolder
                );

            BuddyLogFileDir = Path.Combine(LogFileDir, "Buddy");
            DefaultBuddyLogFileDir = Path.Combine(LogFileDir, "DefaultBuddy");
            
            AutoSaveSettingFilePath = Path.Combine(SaveFileDir, AutoSaveSettingFileName);
            LogFilePath = Path.Combine(LogFileDir, LogTextName);
            
            Directory.CreateDirectory(RootDirectory);
            Directory.CreateDirectory(SaveFileDir);
            Directory.CreateDirectory(LogFileDir);
            Directory.CreateDirectory(BuddyRootDirectory);
        }
    }
}
