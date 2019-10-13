using System.Collections;
using System.IO;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    public class WpfStartAndQuit : MonoBehaviour
    {
        [Inject] private IMessageSender sender = null;
        
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
                //他スクリプトの初期化とUpdateが回ってからの方がよいので少しだけ遅らせる
                yield return null;
                yield return null;
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
