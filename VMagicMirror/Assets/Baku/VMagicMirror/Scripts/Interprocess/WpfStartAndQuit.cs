using System.Collections;
using System.IO;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public class WpfStartAndQuit : MonoBehaviour
    {
        [SerializeField]
        GrpcSender sender = null;

        private static readonly string ConfigExePath = "ConfigApp\\VMagicMirrorConfig.exe";

        private static string GetWpfPath()
            => Path.Combine(
                Path.GetDirectoryName(Application.dataPath),
                ConfigExePath
                );

        void Start()
        {
            StartCoroutine(ActivateWpf());
            Application.wantsToQuit += OnApplicationWantsToQuit;
        }

        private bool OnApplicationWantsToQuit()
        {
            //NOTE: we do not disturb app quit itself, just request config close too.
            sender?.SendCommand(MessageFactory.Instance.CloseConfigWindow());
            return true;
        }

        private IEnumerator ActivateWpf()
        {
            string path = GetWpfPath();
            if (File.Exists(path))
            {
                //Startが全部終わって落ち着いた状態でロードしたいので遅延つける
                yield return new WaitForSeconds(0.5f);
#if !UNITY_EDITOR
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                {
                    FileName = path,
                });
#endif
            }
        }
    }
}
