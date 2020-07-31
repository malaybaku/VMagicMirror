using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Threading.Tasks;
using Baku.VMagicMirror.Mmf;
using Zenject;

namespace Baku.VMagicMirror
{
    public class WpfStartAndQuit : MonoBehaviour
    {
        private const string ConfigProcessName = "VMagicMirrorConfig";
        private static readonly string ConfigExePath = "ConfigApp\\VMagicMirrorConfig.exe";

        private static string GetWpfPath()
            => Path.Combine(
                Path.GetDirectoryName(Application.dataPath),
                ConfigExePath
                );

        private IMessageSender _sender = null;
        private List<IReleaseBeforeQuit> _releaseItems = new List<IReleaseBeforeQuit>();

        private readonly Atomic<bool> _releaseRunning = new Atomic<bool>();
        private readonly Atomic<bool> _releaseCompleted = new Atomic<bool>();

        [Inject]
        public void Initialize(IMessageSender sender, List<IReleaseBeforeQuit> releaseNeededItems)
        {
            _sender = sender;
            _releaseItems = releaseNeededItems;
        }
        
        private void Start()
        {
            StartCoroutine(ActivateWpf());
            //NOTE: WPF側はProcess.CloseMainWindowを使ってUnityを閉じようとする。
            //かつ、Unity側で単体で閉じる方法も今のところはメインウィンドウ閉じのみ。
            Application.wantsToQuit += OnApplicationWantsToQuit;
        }

        private bool OnApplicationWantsToQuit()
        {
            if (_releaseCompleted.Value)
            {
                return true;
            }

            if (_releaseRunning.Value)
            {
                return false;
            }
            _releaseRunning.Value = true;

            //前処理: この時点でMMFとかは既に閉じておく
            foreach (var item in _releaseItems)
            {
                item.ReleaseBeforeCloseConfig();
            }
            
            _sender?.SendCommand(MessageFactory.Instance.CloseConfigWindow());

            //特にリリースするものがないケース: 本来ありえないんだけど、理屈上はほしいので書いておく
            if (_releaseItems.Count == 0)
            {
                _releaseCompleted.Value = true;
                _releaseRunning.Value = false;
                return true;
            }
            
            ReleaseItemsAsync();
            return _releaseCompleted.Value;
        }

        private async void ReleaseItemsAsync()
        {
            await Task.WhenAll(
                _releaseItems.Select(item => item.ReleaseResources())
            );
            
            _releaseCompleted.Value = true;
            _releaseRunning.Value = false;
            //後処理すべきものが実際に片付いたため、閉じてOK。
            Application.Quit();
        }

        private IEnumerator ActivateWpf()
        {
            string path = GetWpfPath();
            if (File.Exists(path))
            {
                //他スクリプトの初期化とUpdateが回ってからの方がよいので少しだけ遅らせる
                yield return null;
                yield return null;
                var startInfo = new ProcessStartInfo()
                {
                    FileName = path,
                    Arguments = "/channelId " + MmfChannelIdSource.ChannelId
                };
#if !UNITY_EDITOR
                Process.Start(startInfo);
                _sender.SendCommand(MessageFactory.Instance.SetUnityProcessId(Process.GetCurrentProcess().Id));
#endif
            }
        }

        private void CloseWpfWindow()
        {
            try
            {
                Process.GetProcesses()
                    .FirstOrDefault(p => p.ProcessName == ConfigProcessName)
                    ?.CloseMainWindow();
            }
            catch (Exception)
            {
                //タイミング的にログ吐くのもちょっと危ないため、やらない
            }
        }
    }
}
