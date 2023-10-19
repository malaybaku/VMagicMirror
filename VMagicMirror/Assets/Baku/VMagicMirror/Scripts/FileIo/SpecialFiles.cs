using System;
using System.IO;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public static class SpecialFiles
    {
        private static readonly string AutoSaveSettingFilePath1;
        
        //セーブファイルには拡張子をつけない(手で触らない想定なため)
        private const string AutoSaveSettingFileName = "_autosave";
        private const string LogTextName = "log.txt";
        
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

        public static string AutoSaveSettingFilePath { get; }

        public static bool AutoSaveSettingFileExists() => File.Exists(AutoSaveSettingFilePath);
        
        public static string ScreenShotDirectory { get; }

        //モーションやテクスチャ差し替えは以下の優先度になることに注意
        //- エディタの場合: StreamingAssets
        //- dev実行: VMM_Dev_Files
        //- prod実行: VMM_Files
        public static string MotionsDirectory => Application.isEditor 
            ? Path.Combine(Application.streamingAssetsPath, "Motions") 
            : Path.Combine(RootDirectory, "Motions");
        
        public static string GetTextureReplacementPath(string textureFileName) => Application.isEditor
            ? Path.Combine(Application.streamingAssetsPath, textureFileName)
            : Path.Combine(RootDirectory, "Textures", textureFileName);
        
        //アクセサリはWPF/Unity双方からディレクトリ走査する都合上、エディタ実行であってもstreamingAssetsは使わない
        public static string AccessoryDirectory => Path.Combine(RootDirectory, "Accessory");

        static SpecialFiles()
        {
            RootDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
                UseDevFolder ? "VMagicMirror_Dev_Files" : "VMagicMirror_Files"
                );

            ScreenShotDirectory = Path.Combine(RootDirectory, "Screenshots");
            SaveFileDir = Path.Combine(RootDirectory, "Saves");
            LogFileDir = Path.Combine(RootDirectory, "Logs");
            AutoSaveSettingFilePath = Path.Combine(SaveFileDir, AutoSaveSettingFileName);
            LogFilePath = Path.Combine(LogFileDir, LogTextName);
            
            Directory.CreateDirectory(RootDirectory);
            Directory.CreateDirectory(SaveFileDir);
            Directory.CreateDirectory(LogFileDir);
        }
    }
}
