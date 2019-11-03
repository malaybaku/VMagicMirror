using System;
using System.IO;
using System.Linq;
using System.Reflection;
using ModestTree;
using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// MODを読み込む処理
    /// </summary>
    public class ModLoader : MonoBehaviour
    {
        private const string ModRootFolderOnExeFile = "Mods";
        private const string ModDllFolderName = "Dlls";
        
        private void Awake()
        {
            LoadMod();
        }

        private void LoadMod()
        {
            //NOTE: Screenshotと違ってフォルダの作成をしない事に注意
            string modFolderPath = GetModFolderPath();
            if (!Directory.Exists(modFolderPath))
            {
                return;
            }

            try
            {
                LoadModMainProcess(modFolderPath);
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }

        //NOTE: MEFっぽい事やるならMEF使った方がいいのでは説
        private void LoadModMainProcess(string modFolderPath)
        {
            //NOTE: とりあえずRecursive Searchはしない方向で。
            foreach (var modFile in Directory
                .GetFiles(modFolderPath)
                .Where(p => Path.GetExtension(p) == ".dll"))
            {
                var asm = Assembly.LoadFile(modFile);
                //コントラクト: 直で読み込むクラスはMonoBehaviourの継承クラスで、かつ[Export]属性がついている事が必須
                var typesToLoad = asm
                    .GetExportedTypes()
                    .Where(t =>
                        t.HasAttribute(typeof(ExportAttribute)) &&
                        typeof(MonoBehaviour).IsAssignableFrom(t))
                    .ToArray();

                foreach (var t in typesToLoad)
                {
                    //何となく見た目に良さげなため、
                    new GameObject().AddComponent(t);
                }
            }
        }

        private static string GetModFolderPath()
        {
            return Path.Combine(
                Path.GetDirectoryName(Application.dataPath),
                ModRootFolderOnExeFile,
                ModDllFolderName
            );
        }
    }

    //NOTE: これ自体DLLで切らないと面倒なことになりそうな。
    [AttributeUsage(AttributeTargets.Class)]
    public class ExportAttribute : Attribute
    {
        public ExportAttribute(string name)
        {
            Name = name ?? "";
        }

        /// <summary> ゲームオブジェクトとして読み込んだときの名前 </summary>
        public string Name { get; }
    }
}
