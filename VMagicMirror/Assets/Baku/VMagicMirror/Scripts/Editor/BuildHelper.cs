using System;
using System.Collections.Generic;
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
        private const string EnvArgPrefix = "-Env=";
        private const string EditionArgPrefix = "-Edition=";

        [MenuItem("VMagicMirror/Prod_Standard_Build", false, 1)]
        public static void DoStandardProdBuild()
        {
            var folder = EditorUtility.SaveFolderPanel("Build Standard Edition", "", "Bin_Standard");
            if (string.IsNullOrEmpty(folder))
            {
                return;
            }
            
            BuildVMagicMirror(folder, false, true);
        }
        
        [MenuItem("VMagicMirror/Prod_Full_Build", false, 2)]
        public static void DoFullProdBuild()
        {
            var folder = EditorUtility.SaveFolderPanel("Build Standard Edition", "", "Bin");
            if (string.IsNullOrEmpty(folder))
            {
                return;
            }
            
            BuildVMagicMirror(folder, true, true);       
        }

        [MenuItem("VMagicMirror/Dev_Standard_Build", false, 21)]
        public static void DoStandardDevBuild()
        {
            var folder = EditorUtility.SaveFolderPanel(
                "(Dev) Build Standard Edition", "", "Bin_Standard_Dev"
                );
            if (string.IsNullOrEmpty(folder))
            {
                return;
            }
            
            BuildVMagicMirror(folder, false, false);
        }
        
        [MenuItem("VMagicMirror/Dev_Full_Build", false, 22)]
        public static void DoFullDevBuild()
        {
            var folder = EditorUtility.SaveFolderPanel(
                "(Dev) Build Full Edition", "", "Bin_Dev"
                );
            if (string.IsNullOrEmpty(folder))
            {
                return;
            }
            
            BuildVMagicMirror(folder, true, false);       
        }

        //NOTE: コマンドラインから使う用。以下のようなオプションをつけて用いる
        //"-SavePath=C:\Hoge\Fuga"
        //"-Env=Prod"
        //"-Edition=Full" 
        public static void DoBuild()
        {
            var savePath = GetSavePathFromArgs();
            var isFullEdition = CheckIsFullEditionFromArgs();
            var isProd = CheckIsProdFromArgs();
            if (!string.IsNullOrEmpty(savePath))
            {
                BuildVMagicMirror(savePath, isFullEdition, isProd);
            }
        }

        private static void BuildVMagicMirror(string folder, bool isFullEdition, bool isProd)
        {
            //NOTE: ビルド直前にスクリプトシンボルを追加し、ビルドしてから元に戻す
            var defineSymbols = PlayerSettings
                .GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup)
                .Split( ';');

            if (!isFullEdition || !isProd)
            {
                var addedSymbols = new List<string>(defineSymbols);
                if (!isFullEdition)
                {
                    addedSymbols.Add("VMM_FEATURE_LOCKED");
                }

                if (!isProd)
                {
                    addedSymbols.Add("DEV_ENV");
                }
                
                PlayerSettings.SetScriptingDefineSymbolsForGroup(
                    EditorUserBuildSettings.selectedBuildTargetGroup,
                    addedSymbols.ToArray()
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

        //prodビルドかどうかをコマンドライン引数から取得します。デフォルトではprod扱いします。
        private static bool CheckIsProdFromArgs()
        {
            var args = Environment.GetCommandLineArgs();
            var pathArg = args.FirstOrDefault(a => a.StartsWith(EnvArgPrefix));
            if (string.IsNullOrEmpty(pathArg))
            {
                Debug.LogWarning("Env is not specified, treat as prod build");
                return true;
            }

            var arg = pathArg.Substring(EnvArgPrefix.Length);
            return string.Compare(arg, "Prod", StringComparison.OrdinalIgnoreCase) == 0;
        }
        
        //Full Editionビルドかどうかをコマンドライン引数から取得します。デフォルトではFull扱いします。
        private static bool CheckIsFullEditionFromArgs()
        {
            var args = Environment.GetCommandLineArgs();
            var pathArg = args.FirstOrDefault(a => a.StartsWith(EditionArgPrefix));
            if (string.IsNullOrEmpty(pathArg))
            {
                Debug.LogWarning("Env is not specified, treat as prod build");
                return true;
            }

            var arg = pathArg.Substring(EnvArgPrefix.Length);
            return string.Compare(arg, "Full", StringComparison.OrdinalIgnoreCase) == 0;
        }
    }
}
