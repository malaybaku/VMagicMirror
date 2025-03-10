using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// <see cref="BuddyApiXmlDocGenerator"/> の処理をビルド前に行うやつ。
    /// </summary>
    public class BuddyApiXmlDocPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder { get; } = 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            BuddyApiXmlDocGenerator.GenerateXml();
        }
    }
        
    /// <summary>
    /// サブキャラ用のAPIを定義したdllのビルド時に得られるxmlファイルをStreamingAssetsに保存するクラス
    /// </summary>
    public static class BuddyApiXmlDocGenerator
    {
        private const string ApiScriptDir = @"Assets\Baku\VMagicMirror\Scripts\Buddy\ApiInterface";
        private static readonly string ApiScriptDirPath = ApiScriptDir.Replace('\\', '/') + "/";

        private const string XmlFileName = StreamingAssetFileNames.BuddyApiXmlDocFileName;
        private static readonly string AssemblyName = Path.ChangeExtension(XmlFileName, ".dll");
        
        private static readonly string OutputDir = Path.Combine("Assets", "StreamingAssets");
        private static readonly string OutputDllPath = Path.Combine(OutputDir, AssemblyName);
        private static readonly string OutputXmlPath = Path.Combine(OutputDir, XmlFileName);
        
        // xmlを生成してStreamingAssetsフォルダに格納する。ビルドはしないがxmlの更新はしたいときに呼び出す
        [MenuItem("VMagicMirror/Generate Buddy API XML")]
        public static void GenerateXmlForBuddyApi()
        {
            GenerateXml();
        }

        /// <summary>
        /// サブキャラ用のAPIのdllおよびxmlをStreamingAssetsフォルダに対して生成したあと、dllファイルを削除する。
        /// </summary>
        /// <returns></returns>
        public static void GenerateXml()
        {
            // NOTE: Api内でさらにフォルダが細分化した場合は再帰的にフォルダを掘る必要もある
            var assemblyBuilder = new AssemblyBuilder(
                OutputDllPath,
                Directory.GetFiles(ApiScriptDir, "*.cs")
                    .Select(filePath => ApiScriptDirPath + Path.GetFileName(filePath)).ToArray()
            )
            {
                // 要らない…よね…？
                //assemblyBuilder.additionalReferences = new[] { assemblyPath };
                compilerOptions = new ScriptCompilerOptions()
                {
                    ApiCompatibilityLevel = ApiCompatibilityLevel.NET_Standard_2_0,
                    CodeOptimization = CodeOptimization.Release,
                    AdditionalCompilerArguments = new[]
                    {
                        $"/doc:{OutputDir}/{XmlFileName}",
                    },
                }
            };

            assemblyBuilder.buildFinished += OnBuildFinished;
            if (!assemblyBuilder.Build())
            {
                Debug.LogError("Assembly build failed.");
            }

            // この処理ではxmlだけに用があるので、dllは直ちに削除しておく
            if (File.Exists(OutputDllPath))
            {
                File.Delete(OutputDllPath);
            }
        }

        // NOTE: 動作が安定したら消してもよい。多分。
        private static void OnBuildFinished(string assemblyPath, CompilerMessage[] messages)
        {
            foreach (var message in messages)
            {
                Debug.Log(message.message);
            }

            if (File.Exists(OutputXmlPath))
            {
                Debug.Log("Xml output was successful");
            }
            else
            {
                Debug.LogWarning("Xml output failed");
            }
        }
    }
}
