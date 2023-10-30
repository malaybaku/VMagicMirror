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
        private const string VmmFeatureLockedSymbol = "VMM_FEATURE_LOCKED";
        private const string DevEnvSymbol = "DEV_ENV";
        
        private const string SavePathArgPrefix = "-SavePath=";
        private const string EnvArgPrefix = "-Env=";
        private const string EditionArgPrefix = "-Edition=";

        [MenuItem("VMagicMirror/Reset Script Symbols", false, 1)]
        public static void ResetScriptSymbols()
        {
            //NOTE: 通常の開発時の組み合わせはScript Symbolが少ないほうに倒しておく
            PrepareScriptDefineSymbol(true, true);
        }
        
        [MenuItem("VMagicMirror/Symbols: Dev Standard", false, 11)]
        public static void PrepareDevStandardSymbols() 
            => PrepareScriptDefineSymbol(false, false);
        [MenuItem("VMagicMirror/Symbols: Prod Full", false, 12)]
        public static void PrepareDeFullSymbols() 
            => PrepareScriptDefineSymbol(true, false);
        
        [MenuItem("VMagicMirror/Symbols: Prod Standard", false, 21)]
        public static void PrepareProdStandardSymbols() 
            => PrepareScriptDefineSymbol(false, true);
        [MenuItem("VMagicMirror/Symbols: Prod Full", false, 22)]
        public static void PrepareProdFullSymbols() 
            => PrepareScriptDefineSymbol(true, true);
        
        [MenuItem("VMagicMirror/Build", false, 31)]
        public static void TryBuild()
        {
            var folder = EditorUtility.SaveFolderPanel("Build Standard Edition", "", "Bin_Standard");
            if (string.IsNullOrEmpty(folder))
            {
                return;
            }
            
            BuildVMagicMirror(folder);
        }
        
        //NOTE: コマンドラインから使う用で、ビルドの前に1回これだけ呼んでEditorを終了しておくことにより、Symbol更新のタイミングバグを防ぐ
        //"-Env=Prod"
        //"-Edition=Full" 
        public static void DoPrepareScriptDefineSymbol()
        {
            var isFullEdition = CheckIsFullEditionFromArgs();
            var isProd = CheckIsProdFromArgs();
            PrepareScriptDefineSymbol(isFullEdition, isProd);
        }
        
        //NOTE: コマンドラインから使う用。以下のようなオプションをつけて用いる。
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
                BuildVMagicMirror(savePath);
            }

            if (isProd)
            {
                RemoveBurstRelatedFolder(savePath);
            }
        }

        private static void PrepareScriptDefineSymbol(bool isFullEdition, bool isProd)
        {
            //NOTE: スクリプトシンボルに過不足があれば直す。追加だけでなく削除も含むことに注意
            var symbols = PlayerSettings
                .GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup)
                .Split(';')
                .ToList();
            var isDirty = false;
            
            if (!isFullEdition && !symbols.Contains(VmmFeatureLockedSymbol))
            {
                symbols.Add(VmmFeatureLockedSymbol);
                isDirty = true;
            }

            if (isFullEdition && symbols.Contains(VmmFeatureLockedSymbol))
            {
                symbols.Remove(VmmFeatureLockedSymbol);
                isDirty = true;
            }

            if (!isProd && !symbols.Contains(DevEnvSymbol))
            {
                symbols.Add(DevEnvSymbol);
                isDirty = true;
            }

            if (isProd && symbols.Contains(DevEnvSymbol))
            {
                symbols.Remove(DevEnvSymbol);
                isDirty = true;
            }


            if (isDirty)
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(
                    EditorUserBuildSettings.selectedBuildTargetGroup,
                    symbols.ToArray()
                );
            }
        }
        
        private static void BuildVMagicMirror(string folder)
        {
            var savePath = Path.Combine(folder, "VMagicMirror.exe");
            BuildPipeline.BuildPlayer(
                EditorBuildSettings.scenes.Where(s => s.enabled).ToArray(),
                savePath,
                BuildTarget.StandaloneWindows64,
                BuildOptions.None
            );
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

        private static void RemoveBurstRelatedFolder(string savePath)
        {
            var burstDir = Path.Combine(savePath, "VMagicMirror_BurstDebugInformation_DoNotShip");
            if (Directory.Exists(burstDir))
            {
                Directory.Delete(burstDir, true);
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

            var arg = pathArg.Substring(EditionArgPrefix.Length);
            return string.Compare(arg, "Full", StringComparison.OrdinalIgnoreCase) == 0;
        }
    }
}
