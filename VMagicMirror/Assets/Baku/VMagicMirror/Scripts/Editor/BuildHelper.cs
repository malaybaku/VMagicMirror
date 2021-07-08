using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public static class BuildHelper
    {
        private const string SavePathArgPrefix = "-SavePath=";


        [MenuItem("VMagicMirror/Standard_Build")]
        public static void DoStandardBuild()
        {
            var folder = EditorUtility.SaveFolderPanel("Build Standard Edition", "", "Bin_Standard");
            if (string.IsNullOrEmpty(folder))
            {
                return;
            }
            
            BuildVMagicMirror(folder, false);
        }
        
        [MenuItem("VMagicMirror/Full_Build")]
        public static void DoFullBuild()
        {
            var folder = EditorUtility.SaveFolderPanel("Build Standard Edition", "", "Bin");
            if (string.IsNullOrEmpty(folder))
            {
                return;
            }
            
            BuildVMagicMirror(folder, true);       
        }
        
        
        //NOTE: コマンドラインから使う用。"SavePath=C:\Hoge\Fuga"のようなコマンドライン引数によって保存先を指定する
        public static void DoStandardBuildWithPath()
        {
            var savePath = GetSavePathFromArgs();
            if (!string.IsNullOrEmpty(savePath))
            {
                BuildVMagicMirror(savePath, false);
            }
        }

        public static void DoFullBuildWithPath()
        {
            var savePath = GetSavePathFromArgs();
            if (!string.IsNullOrEmpty(savePath))
            {
                BuildVMagicMirror(savePath, true);
            }
        }

        private static void BuildVMagicMirror(string folder, bool isFullEdition)
        {
            //NOTE: ビルド直前にスクリプトシンボルを追加し、ビルドしてから元に戻す
            var defineSymbols = PlayerSettings
                .GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup)
                .Split( ';');
            
            if (!isFullEdition)
            {
                var featureLockSymbols = new string[defineSymbols.Length + 1];
                Array.Copy(defineSymbols, featureLockSymbols, defineSymbols.Length);
                featureLockSymbols[featureLockSymbols.Length - 1] = "VMM_FEATURE_LOCKED";
                PlayerSettings.SetScriptingDefineSymbolsForGroup(
                    EditorUserBuildSettings.selectedBuildTargetGroup,
                    featureLockSymbols
                );
            }

            try
            {
                string savePath = Path.Combine(folder, "VMagicMirror.exe");
                BuildPipeline.BuildPlayer(
                    EditorBuildSettings.scenes.Where(s => s.enabled).ToArray(),
                    savePath,
                    BuildTarget.StandaloneWindows64,
                    BuildOptions.None
                );
            }
            finally
            {
                if (!isFullEdition)
                {
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(
                        EditorUserBuildSettings.selectedBuildTargetGroup,
                        defineSymbols
                    );
                }
            }
        }

        [PostProcessBuild(1)]
        public static void RemoveUnnecessaryFilesFromStreamingAssets(BuildTarget target, string savePath)
        {
            if (target != BuildTarget.StandaloneWindows64 && target != BuildTarget.StandaloneWindows)
            {
                return;
            }

            var rootFolder = Path.GetDirectoryName(savePath);

            string streamingAssetDir = Path.Combine(rootFolder, "VMagicMirror_Data", "StreamingAssets");
            if (!Directory.Exists(streamingAssetDir))
            {
                Debug.LogWarning("Folder was not found: " + streamingAssetDir);
                return;
            }

            //VRMLoaderUI以外のディレクトリは削除
            foreach (var dir in Directory.GetDirectories(streamingAssetDir))
            {
                if (Path.GetFileName(dir) != StreamingAssetFileNames.LoaderUiFolder)
                {
                    Directory.Delete(dir, true);
                }
            }
            
            //顔トラッキングのモデルファイル以外のファイルも削除
            foreach (var file in Directory.GetFiles(streamingAssetDir))
            {
                string fileName = Path.GetFileName(file);
                if (fileName != StreamingAssetFileNames.DnnModelFileName &&
                    fileName != StreamingAssetFileNames.DlibFaceTrackingDataFileName)
                {
                    File.Delete(file);
                }
            }

            //ついでにログファイルも削除
            var logFile = Path.Combine(rootFolder, "log.txt");
            if (File.Exists(logFile))
            {
                File.Delete(logFile);
            }

            var logConfigFile = Path.Combine(rootFolder, "log_config.txt");
            if (File.Exists(logConfigFile))
            {
                File.Delete(logConfigFile);
            }
        }

        private static string GetSavePathFromArgs()
        {
            var args = Environment.GetCommandLineArgs();
            var pathArg = args.FirstOrDefault(a => a.StartsWith(SavePathArgPrefix));
            if (string.IsNullOrEmpty(pathArg))
            {
                Debug.LogError(
                    "Save Path is not specified. Set it by arg like '" +
                    SavePathArgPrefix + "C:\\path_to\\save\\folder'"
                );
                return "";
            }
            
            return pathArg.Substring(SavePathArgPrefix.Length);
        }
    }
}
