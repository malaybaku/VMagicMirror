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
        
        private static string RootDirectory { get; }
        private static string SaveFileDir { get; }
        public static string LogFileDir { get; }
        public static string LogFilePath { get; }

        public static string AutoSaveSettingFilePath { get; }

        public static bool AutoSaveSettingFileExists() => File.Exists(AutoSaveSettingFilePath);
        
        //NOTE: エディタではdataPathがAssets/以下になって都合が悪いのでデスクトップに逃がす
        public static string ScreenShotDirectory => Application.isEditor
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Screenshots") 
            : Path.Combine(RootDirectory, "Screenshots");
        
        //モーションだけはエディタの場合StreamingAssets以下で代用する。Desktopに置くと揮発しすぎるため
        public static string MotionsDirectory => Application.isEditor 
            ? Path.Combine(Application.streamingAssetsPath, "Motions") 
            : Path.Combine(RootDirectory, "Motions");
        
        public static string GetTextureReplacementPath(string textureFileName) => Application.isEditor
            ? Path.Combine(Application.streamingAssetsPath, textureFileName)
            : Path.Combine(RootDirectory, "Textures", textureFileName);
        
        static SpecialFiles()
        {
            Environment.SpecialFolder rootParent = Application.isEditor
                ? Environment.SpecialFolder.Desktop 
                : Environment.SpecialFolder.MyDocuments;
            RootDirectory = Path.Combine(Environment.GetFolderPath(rootParent), "VMagicMirror_Files");

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
