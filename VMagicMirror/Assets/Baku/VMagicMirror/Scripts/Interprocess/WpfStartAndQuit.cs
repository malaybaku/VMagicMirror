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
using Debug = UnityEngine.Debug;

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
        private List<IReleaseBeforeQuit> _releaseItems = new();

        private readonly Atomic<bool> _releaseRunning = new();
        private readonly Atomic<bool> _releaseCompleted = new();

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
            //他スクリプトの初期化とUpdateが回ってからの方がよいので少しだけ遅らせる
            yield return null;
            yield return null;

            if (Application.isEditor)
            {
                try
                {
                    ActivateWpfFromEditor();
                }
                catch (Exception ex)
                {
                    // ログは出すけど想定内エラーということにする
                    Debug.LogException(ex);
                }
                yield break;
            }
            
            var path = GetWpfPath();
            if (!File.Exists(path))
            {
                yield break;
            }
            
            StartProcess(path);
            // WPF側のウィンドウ閉じでUnity側が閉じられるようにProcess Idを教えておく。これはEditorの場合は不要
            _sender.SendCommand(MessageFactory.Instance.SetUnityProcessId(Process.GetCurrentProcess().Id));
        }

        private void ActivateWpfFromEditor()
        {
            // pj直下にデバッグ用のexeのパスを書いたファイルを置いておき、それを読みに行く
            var editorWpfExePathFile = Path.Combine(
                Path.GetDirectoryName(Application.dataPath),
                "debug_wpf_exe_path.txt"
            );

            var exePath = "";
            if (File.Exists(editorWpfExePathFile))
            {
                exePath = File.ReadAllText(editorWpfExePathFile);
            }

            if (!File.Exists(exePath))
            {
                return;
            }

            // NOTE: ビルド版と異なり、Process Idは教えない(= WPF側はUnity Editorを終了させないようにしとく)
            StartProcess(exePath);
        }

        private void StartProcess(string exePath)
        {
            var startInfo = new ProcessStartInfo()
            {
                FileName = exePath,
                Arguments = "/channelId " + MmfChannelIdSource.ChannelId
            };
            Process.Start(startInfo);
        }
    }
}
