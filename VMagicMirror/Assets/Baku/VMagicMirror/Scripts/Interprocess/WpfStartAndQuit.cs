using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;
using System.Threading.Tasks;
using Baku.VMagicMirror.Mmf;
using Cysharp.Threading.Tasks;
using Zenject;
using Debug = UnityEngine.Debug;

namespace Baku.VMagicMirror
{
    public class WpfStartAndQuit : MonoBehaviour
    {
        private const string ConfigExePath = "ConfigApp\\VMagicMirrorConfig.exe";

        private static string GetWpfPath()
            => Path.Combine(
                Path.GetDirectoryName(Application.dataPath),
                ConfigExePath
                );
        private IMessageSender _sender;

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
            ActivateWpfAsync(this.GetCancellationTokenOnDestroy()).Forget();
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

            //前処理
            foreach (var item in _releaseItems)
            {
                item.ReleaseBeforeCloseConfig();
            }

            // NOTE: UnityではこのコマンドだけがLast扱い
            _sender.SendCommand(MessageFactory.CloseConfigWindow(), true);

            //特にリリースするものがないケース: 本来ありえないんだけど、理屈上はほしいので書いておく
            if (_releaseItems.Count == 0)
            {
                _releaseCompleted.Value = true;
                _releaseRunning.Value = false;
                return true;
            }
            
            ReleaseItemsAsync().Forget();
            return _releaseCompleted.Value;
        }

        private async UniTaskVoid ReleaseItemsAsync()
        {
            // - WPFを終了させるコマンドの送信完了を待つ
            // - このとき、WPFが想定外の事情で先に完全終了してると進まないことがあるので、そこはTimeoutで捌く
            var cancellationToken = this.GetCancellationTokenOnDestroy();
            try
            {
                await UniTask
                    .WaitUntil(() => _sender.LastMessageSent, cancellationToken: cancellationToken)
                    .Timeout(TimeSpan.FromSeconds(1));
            }
            catch (TimeoutException)
            {
            }
            catch (OperationCanceledException)
            {
                // コッチはEditorでのみ通過するはず
            }
            
            await Task.WhenAll(
                _releaseItems.Select(item => item.ReleaseResources())
            );
            
            _releaseCompleted.Value = true;
            _releaseRunning.Value = false;
            //後処理すべきものが実際に片付いたため、閉じてOK。
            Application.Quit();
        }

        private async UniTaskVoid ActivateWpfAsync(CancellationToken token)
        {
            //他スクリプトの初期化とUpdateが回ってからの方がよいので少しだけ遅らせる
            await UniTask.DelayFrame(2, cancellationToken: token);

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
                return;
            }
            
            var path = GetWpfPath();
            if (!File.Exists(path))
            {
                return;
            }
            
            StartProcess(path);
            // WPF側のウィンドウ閉じでUnity側が閉じられるようにProcess Idを教えておく。これはEditorの場合は不要
            _sender.SendCommand(MessageFactory.SetUnityProcessId(Process.GetCurrentProcess().Id));
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
            var args = "/channelId " + MmfChannelIdSource.ChannelId;
            if (Application.isEditor)
            {
                // NOTE:
                // - サブキャラのフォルダをWPFがreadできてほしい & Editor実行だとStreamingAssetsの位置が非自明なので伝える
                //   - ビルドの場合、WPFとUnityのフォルダ構造は確定しているので、伝える必要がなくなる
                // - WPFから飛んでくるサブキャラ関連のフォルダ名をバックスラッシュ区切りに統一しておきたいので、
                //   この時点でバックスラッシュ区切りに寄せておく
                args += " /streamingAssetsDir " + Application.streamingAssetsPath.Replace('/', '\\');
            }

            var startInfo = new ProcessStartInfo()
            {
                FileName = exePath,
                Arguments = args,
            };
            Process.Start(startInfo);
        }
    }
}
