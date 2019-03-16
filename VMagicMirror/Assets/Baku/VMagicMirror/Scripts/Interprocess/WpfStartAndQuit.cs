using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public class WpfStartAndQuit : MonoBehaviour
    {
        private static readonly string ConfigExePath = "ConfigApp\\VMagicMirrorConfig.exe";

        private static string GetWpfPath()
            => Path.Combine(
                Path.GetDirectoryName(Application.dataPath),
                ConfigExePath
                );

        void Start()
        {
            //Startが全部終わって落ち着いた状態でロードしたいので遅延つける
            StartCoroutine(ActivateWpf());
        }

        private void OnDestroy()
        {
            //いったん停止: この方法で止めると設定ファイルが保存されないため
#if !UNITY_EDITOR
            //Process.GetProcesses()
            //    .FirstOrDefault(p => p.ProcessName == "VMagicMirrorConfig")
            //    ?.CloseMainWindow();
#endif
        }

        private IEnumerator ActivateWpf()
        {
            string path = GetWpfPath();
            if (File.Exists(path))
            {
                yield return new WaitForSeconds(0.5f);
#if !UNITY_EDITOR
                Process.Start(new ProcessStartInfo()
                {
                    FileName = path,
                });
#endif
            }
        }
    }
}
