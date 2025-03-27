using System;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    public readonly struct WebCamImageSource
    {
        public WebCamImageSource(
            Mediapipe.Unity.Experimental.TextureFrame textureFrame,
            WebCamTexture webCamTexture,
            long timestampMilliseconds)
        {
            _textureFrame = textureFrame;
            _webCamTexture = webCamTexture;
            TimestampMilliseconds = timestampMilliseconds;
        }

        private readonly Mediapipe.Unity.Experimental.TextureFrame _textureFrame;
        private readonly WebCamTexture _webCamTexture;

        // NOTE: 呼び出した側のコードはusingをつけて破棄に責任を持つ必要がある
        public Mediapipe.Image BuildImage()
        {
            _textureFrame.ReadTextureOnCPU(_webCamTexture, flipHorizontally: false, flipVertically: true);
            return _textureFrame.BuildCPUImage();
        }

        // NOTE?: Task側がStop/Startをした場合、ここの値が急にデカい値であることによって困ったりするかも
        public long TimestampMilliseconds { get; }
    }
    
    /// <summary>
    /// Mediapipeのタスクの基盤処理として、WebCamTextureを起動してカメラ画像が更新されたらIObservableで発火するやつ
    /// </summary>
    public class WebCamTextureSource : MonoBehaviour
    {
        [SerializeField] private WebCamSettings settings;
        [SerializeField] private RawImage screen;
        [SerializeField] private bool showFpsLog;

        private CancellationTokenSource _cts;
        private WebCamTexture _webCamTexture;
        private int _imageUpdatedCount;
        private long _imageUpdatedPrevTimestamp;

        private readonly Subject<WebCamImageSource> _imageUpdated = new();

        /// <summary>
        /// TODO: これだと「発火した値をキャッシュしちゃダメ」という仕様になるが、マルチスレッドと相性が悪いかも。
        /// WebCamImageの(というか、その中のImageの)キャッシュを次の didUpdateThisFrame まで持たすような構造は有りそう。
        /// </summary>
        public IObservable<WebCamImageSource> ImageUpdated => _imageUpdated;

        public int Width { get; private set; }
        public int Height { get; private set; }

        // NOTE: start/stopメソッドはVMMへの移植を想定して書いているが、VMMに持ってくまでは使わないでヨイ
        public void StartWebCam(string deviceName)
        {
            StopWebCam();
            var index = GetDeviceIndex(deviceName);
            _cts = new CancellationTokenSource();
            CaptureWebCamTextureAsync(index, _cts.Token).Forget();
        }

        public void StopWebCam()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            if (_webCamTexture != null)
            {
                _webCamTexture.Stop();
            }
            _webCamTexture = null;
        }        
        
        // NOTE: VMM本体の場合、このStartが不要になって、他クラスからStart/Stopしてほしい感じになる(& MonoBehaviourでもなくなるはず)
        private void Start()
        {
            Width = settings.Width;
            Height = settings.Height;

            var index = GetDeviceIndex(settings.PreferredName);
            if (index < 0)
            {
                index = 0;
            }
            _cts = new CancellationTokenSource();
            
            CaptureWebCamTextureAsync(index, _cts.Token).Forget();
        }

        private void OnDestroy() => StopWebCam();

        private async UniTaskVoid CaptureWebCamTextureAsync(int deviceIndex, CancellationToken cancellationToken)
        {
            await PrepareWebCamTextureAsync(deviceIndex, cancellationToken);
            
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // NOTE: 本家？の実装でも1F待ってるので合わせている
            await UniTask.NextFrame(cancellationToken);
            using var textureFrame = new Mediapipe.Unity.Experimental.TextureFrame(
                _webCamTexture.width, _webCamTexture.height, TextureFormat.RGBA32
                );

            while (!cancellationToken.IsCancellationRequested)
            {
                if (!_webCamTexture.didUpdateThisFrame)
                {
                    await UniTask.DelayFrame(1, PlayerLoopTiming.PreLateUpdate, cancellationToken);
                    continue;
                }

                LogImageUpdated(stopwatch.ElapsedMilliseconds);
                // textureFrame.ReadTextureOnCPU(_webCamTexture, flipHorizontally: false, flipVertically: true);
                // using var image = textureFrame.BuildCPUImage();
                // _imageUpdated.OnNext(new WebCamImage(image, stopwatch.ElapsedMilliseconds));
                _imageUpdated.OnNext(new WebCamImageSource(textureFrame, _webCamTexture, stopwatch.ElapsedMilliseconds));

                await UniTask.DelayFrame(1, PlayerLoopTiming.PreLateUpdate, cancellationToken);
            }
        }

        private void LogImageUpdated(long timestampMillisecond)
        {
            // NOTE: カウント自体は常時行うが、ログ出力をするのはオプションで有効になっているときだけ…というくらいにしておく
            // (別に最適化したいわけじゃないからね)
            _imageUpdatedCount++;
            if (_imageUpdatedCount % 30 == 0)
            {
                var elapsedTime = (timestampMillisecond - _imageUpdatedPrevTimestamp) * 0.001f;
                var rate = 30 / elapsedTime;
                if (showFpsLog)
                {
                    Debug.Log($"WebCamTexture DidUpdate 30 times, Elapsed={elapsedTime:0.000}, fps={rate:0.0}");
                }
                _imageUpdatedPrevTimestamp = timestampMillisecond;
            }
        }
        private async UniTask PrepareWebCamTextureAsync(int deviceIndex, CancellationToken cancellationToken)
        {
            if (WebCamTexture.devices.Length == 0)
            {
                Debug.LogError("Web Camera devices are not found");
                return;
            }

            var webCamDevice = WebCamTexture.devices[deviceIndex];
            _webCamTexture = new WebCamTexture(webCamDevice.name, settings.Width, settings.Height, settings.Fps);
            _webCamTexture.Play();

            // NOTE: MacOS用の処置らしいのでなくても済むかもしれないが、あってもとくに困らないので入れている 
            await UniTask.WaitUntil(() => _webCamTexture.width > 16, cancellationToken: cancellationToken);

            screen.texture = _webCamTexture;
            Width = _webCamTexture.width;
            Height = _webCamTexture.height;
            Debug.Log($"WebCamTexture (w,h), request=({settings.Width},{settings.Height}), actual=({Width},{Height})");
        }
        
        // NOTE: 一致しない場合は-1を返す
        private static int GetDeviceIndex(string deviceName)
        {
            if (WebCamTexture.devices.Length == 0)
            {
                Debug.LogWarning("No WebCam device");
                return -1;
            }

            var devices = WebCamTexture.devices;
            foreach (var d in devices)
            {
                Debug.Log("WebCam Device Name: " + d.name);
            }

            var deviceIndex = Array.FindIndex(devices, d => d.name == deviceName);
            return deviceIndex >= 0 ? deviceIndex : -1;
        }
    }
}
