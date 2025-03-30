using System;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
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
    public class WebCamTextureSource : IDisposable
    {
        // NOTE: debug中だけtrueにする
        private const bool ShowFpsLog = false;

        private readonly WebCamSettings _settings;
        
        private CancellationTokenSource _cts;
        private WebCamTexture _webCamTexture;
        private int _imageUpdatedCount;
        private long _imageUpdatedPrevTimestamp;

        private readonly Subject<WebCamImageSource> _imageUpdated = new();
        /// <summary>
        /// Webカメラのテクスチャが更新されたフレームで発火する。
        /// 購読側では発火に対してDelayつきで画像を読み出そうとしたとき、正しく読めることは保証されない。
        /// </summary>
        public IObservable<WebCamImageSource> ImageUpdated => _imageUpdated;

        public int Width { get; private set; }
        public int Height { get; private set; }

        [Inject]
        public WebCamTextureSource(WebCamSettings settings)
        {
            _settings = settings;

            // 初期値がゼロだと流石にアレなので値を入れておく
            Width = _settings.Width;
            Height = _settings.Height;
        }

        /// <summary>
        /// Webカメラのテクスチャの取得を開始、または停止する。<paramref name="deviceName"/>は開始する場合のみ値が使われる
        /// </summary>
        /// <param name="active"></param>
        /// <param name="deviceName"></param>
        public void SetActive(bool active, string deviceName)
        {
            if (active)
            {
                StartWebCam(deviceName);
            }
            else
            {
                StopWebCam();
            }
        }
        
        // NOTE: start/stopメソッドはVMMへの移植を想定して書いているが、VMMに持ってくまでは使わないでヨイ
        private void StartWebCam(string deviceName)
        {
            StopWebCam();
            var index = GetDeviceIndex(deviceName);
            _cts = new CancellationTokenSource();
            CaptureWebCamTextureAsync(index, _cts.Token).Forget();
        }

        private void StopWebCam()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            if (_webCamTexture != null)
            {
                _webCamTexture.Stop();
            }

            _webCamTexture = null;
            // NOTE: ここでWidth/Heightをリセットしてもいいが、しないでも破綻しないはずなので放っておく
        }

        void IDisposable.Dispose() => StopWebCam();

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
            if (!ShowFpsLog)
            {
                return;
            }

            _imageUpdatedCount++;
            if (_imageUpdatedCount % 30 == 0)
            {
                var elapsedTime = (timestampMillisecond - _imageUpdatedPrevTimestamp) * 0.001f;
                var rate = 30 / elapsedTime;
                Debug.Log($"WebCamTexture DidUpdate 30 times, Elapsed={elapsedTime:0.000}, fps={rate:0.0}");
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
            _webCamTexture = new WebCamTexture(webCamDevice.name, _settings.Width, _settings.Height, _settings.Fps);
            _webCamTexture.Play();

            // NOTE: MacOS用の処置らしいのでなくても済むかもしれないが、あってもとくに困らないので入れている 
            await UniTask.WaitUntil(() => _webCamTexture.width > 16, cancellationToken: cancellationToken);

            Width = _webCamTexture.width;
            Height = _webCamTexture.height;
            LogOutput.Instance.Write($"MediaPipeTracker: WebCamTexture (w,h), request=({_settings.Width},{_settings.Height}), actual=({Width},{Height})");
        }
        
        // NOTE: 一致しない場合は-1を返す
        private static int GetDeviceIndex(string deviceName)
        {
            if (WebCamTexture.devices.Length == 0)
            {
                return -1;
            }

            var devices = WebCamTexture.devices;
            var deviceIndex = Array.FindIndex(devices, d => d.name == deviceName);
            return deviceIndex >= 0 ? deviceIndex : -1;
        }
    }
}
