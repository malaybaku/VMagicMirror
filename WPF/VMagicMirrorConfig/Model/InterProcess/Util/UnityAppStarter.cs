using System;
using System.IO;

namespace Baku.VMagicMirrorConfig
{
    internal static class UnityAppStarter
    {
        /// <summary>
        /// VMagicMirrorを再起動したいとき、いま起動しているアプリがほぼ終了した状態で呼び出すことにより、
        /// Unityのプロセスを新規に立ち上げます。
        /// </summary>
        public static void StartUnityApp()
        {
            var location = Environment.ProcessPath;
            if (string.IsNullOrEmpty(location))
            {
                LogOutput.Instance.Write("Coult not restart unity process, because self process path was not specified");
                return;
            }
            
            var parentDir = Path.GetDirectoryName(Path.GetDirectoryName(location));
            if (!Directory.Exists(parentDir))
            {
                LogOutput.Instance.Write("Could not restart unity process, because exe folder was not specified");
                return;
            }

            var exePath = Path.Combine(parentDir, "VMagicMirror.exe");
            if (!File.Exists(exePath))
            {
                LogOutput.Instance.Write("Could not restart unity process, because exe file was not specified");
                return;
            }

            System.Diagnostics.Process.Start(exePath);
        }
    }
}
