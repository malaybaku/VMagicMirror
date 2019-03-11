using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public class WpfActivator : MonoBehaviour
    {
        private static readonly string ConfigExePath = "..\\ConfigApp\\VMagicMirrorConfig.exe";

        private static string GetWpfPath() 
            => Path.Combine(
                Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
                ConfigExePath
                );

        void Start()
        {
            //Startが全部終わって落ち着いた状態でロードしたいので遅延付ける
            StartCoroutine(ActivateWpf());
        }

        private IEnumerator ActivateWpf()
        {
            string path = GetWpfPath();

            yield return new WaitForSeconds(1.0f);

            if (File.Exists(path))
            {
                Process.Start(path);
            }
        }
    }
}
