using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Baku.VMagicMirror
{
    public enum SpoutResolutionType
    {
        //TwiceAsScreenみたいのも考えうるが、一旦忘れる
        SameAsScreen = 0,
        Fixed1280 = 1,
        Fixed1920 = 2,
        Fixed2560 = 3,
        Fixed3840 = 4,
    }

    //TODO: Spoutにだけ背景透過で描画投げたい需要があるらしいが対応するかどうか
    // -> 個人的にはあまり対応したくないが…
    public class SpoutSenderController : PresenterBase
    {
        private readonly Camera _mainCamera;
        private readonly SpoutSenderWrapperView _view;
        private readonly IMessageReceiver _messageReceiver;

        private readonly ReactiveProperty<bool> _isActive = new ReactiveProperty<bool>(false);

        private readonly ReactiveProperty<SpoutResolutionType> _resolutionType 
            = new ReactiveProperty<SpoutResolutionType>(SpoutResolutionType.SameAsScreen);

        private RenderTexture _renderTexture = null;
        private CancellationTokenSource _textureSizePollingCts;
        
        public SpoutSenderController(
            Camera mainCamera,
            SpoutSenderWrapperView view,
            IMessageReceiver messageReceiver
            )
        {
            _mainCamera = mainCamera;
            _view = view;
            _messageReceiver = messageReceiver;
        }

        public override void Initialize()
        {
            _view.InitializeSpoutSender();

            _messageReceiver.AssignCommandHandler(
                VmmCommands.EnableSpoutOutput,
                command => SetSpoutActiveness(command.ToBoolean())
                );
         
            _messageReceiver.AssignCommandHandler(
                VmmCommands.SetSpoutOutputResolution,
                command => SetSpoutResolutionType(command.ToInt()));
            _messageReceiver.AssignCommandHandler(
                VmmCommands.ShowSpoutOutputToWindow,
                command => SetSpoutOutputToWindowActive(command.ToBoolean())
                );

            _resolutionType
                .Subscribe(_ => RefreshRenderTexture())
                .AddTo(this);

            _isActive
                .CombineLatest(_resolutionType, (active, type) => (active && type == SpoutResolutionType.SameAsScreen))
                .DistinctUntilChanged()
                .Subscribe(pollRenderTextureUpdate =>
                {
                    if (pollRenderTextureUpdate)
                    {
                        StopPollRenderTextureSizeAdjust();
                        _textureSizePollingCts = new CancellationTokenSource();
                        PollRenderTextureSizeAdjustAsync(_textureSizePollingCts.Token).Forget();
                    }
                    else
                    {
                        StopPollRenderTextureSizeAdjust();
                    }
                })
                .AddTo(this);
        }

        public override void Dispose()
        {
            base.Dispose();
            StopPollRenderTextureSizeAdjust();
        }

        private void SetSpoutActiveness(bool active)
        {
            Debug.Log($"SetSpoutActiveness: {active}");
            _isActive.Value = active;
            _view.SetSpoutSenderActive(active);
            _view.SetOverwriteObjectsActive(active);
            RefreshRenderTexture();
        }

        private void SetSpoutResolutionType(int rawType)
        {
            //ダウングレードしたユーザー環境で起きうる: 起きたら無視
            if (rawType < 0 || rawType > (int)SpoutResolutionType.Fixed3840)
            {
                return;
            }

            var type = (SpoutResolutionType)rawType;
            if (type == _resolutionType.Value)
            {
                return;
            }

            _resolutionType.Value = type; 
            RefreshRenderTexture();
        }

        private void RefreshRenderTexture()
        {
            DisposeTexture();
            if (!_isActive.Value)
            {
                return;
            }
            var resolutionType = _resolutionType.Value;
            
            
            var (width, height) = GetResolution(resolutionType);
            InitializeRenderTexture(width, height);
        }

        private (int width, int height) GetResolution(SpoutResolutionType type)
        {
            switch (type)
            {
                case SpoutResolutionType.Fixed1280: return (1280, 720);
                case SpoutResolutionType.Fixed1920: return (1920, 1080);
                case SpoutResolutionType.Fixed2560: return (2560, 1440);
                case SpoutResolutionType.Fixed3840: return (3840, 2160);
                default: return (Screen.width, Screen.height);
            }           
        }

        private void SetSpoutOutputToWindowActive(bool active)
        {
            //TODO: カメラの起動やら何やら?
        }

        //画面サイズにテクスチャサイズを揃えるのを行う。ウィンドウリサイズ中の更新頻度は抑える
        private async UniTaskVoid PollRenderTextureSizeAdjustAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: cancellationToken);

                var w = Screen.width;
                var h = Screen.height;
                if (_renderTexture.width == w && _renderTexture.height == h)
                {
                    continue;
                }
                
                DisposeTexture();
                InitializeRenderTexture(w, h);
            }
        }

        private void StopPollRenderTextureSizeAdjust()
        {
            _textureSizePollingCts?.Cancel();
            _textureSizePollingCts?.Dispose();
            _textureSizePollingCts = null;
        }

        private void InitializeRenderTexture(int width, int height)
        {
            //alphaを保持する、影とかも込みで送信したいため
            _renderTexture = new RenderTexture(width, height, 32, RenderTextureFormat.ARGB32);
            _mainCamera.targetTexture = _renderTexture;
            _view.SetTexture(_renderTexture);
        }
        
        private void DisposeTexture()
        {
            if (_renderTexture != null)
            {
                Object.Destroy(_renderTexture);
                _renderTexture = null;
            }

            if (_view != null)
            {
                _view.SetTexture(null);
            }

            if (_mainCamera != null)
            {
                _mainCamera.targetTexture = null;
            }
        }
    }
}
