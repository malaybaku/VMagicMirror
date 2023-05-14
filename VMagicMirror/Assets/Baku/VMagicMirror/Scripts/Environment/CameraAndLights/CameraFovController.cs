using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    public class CameraFovController : PresenterBase
    {
        //NOTE: 直近で可変値を想定しないでいいので定数にしている
        private const float SpoutVideoAspect = 16f / 9f;

        private readonly Camera _mainCamera;
        private readonly Camera _cameraForRay;
        private readonly IMessageReceiver _receiver;
        private readonly SpoutSenderController _spoutSenderController;

        private readonly ReactiveProperty<float> _baseFov = new ReactiveProperty<float>(40f);

        private CancellationTokenSource _adjustFovCts;
        
        public CameraFovController(
            Camera mainCamera,
            [Inject(Id = "RefCameraForRay")] Camera cameraForRay,
            IMessageReceiver receiver, 
            SpoutSenderController spoutSenderController)
        {
            _mainCamera = mainCamera;
            _cameraForRay = cameraForRay;
            _receiver = receiver;
            _spoutSenderController = spoutSenderController;
        }

        public override void Initialize()
        {
            _baseFov.Value = _mainCamera.fieldOfView;

            _receiver.AssignCommandHandler(
                VmmCommands.CameraFov, 
                command => _baseFov.Value = command.ToInt()
                );

            _baseFov
                .Subscribe(fov =>
                {
                    _cameraForRay.fieldOfView = fov;
                    if (_spoutSenderController.NeedFovModify.Value)
                    {
                        _mainCamera.fieldOfView = GetModifiedFov(fov);
                    }
                    else
                    {
                        //こっちがメジャーケース
                        _mainCamera.fieldOfView = fov;
                    }
                })
                .AddTo(this);

            _spoutSenderController.NeedFovModify
                .Subscribe(needModify =>
                {
                    if (needModify)
                    {
                        StopAdjustFov();
                        _adjustFovCts = new CancellationTokenSource();
                        AdjustFovAsync(_adjustFovCts.Token).Forget();
                    }
                    else
                    {
                        StopAdjustFov();
                        _mainCamera.fieldOfView = _baseFov.Value;
                    }
                })
                .AddTo(this);
        }

        public override void Dispose()
        {
            base.Dispose();
            StopAdjustFov();
        }

        private async UniTaskVoid AdjustFovAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                _mainCamera.fieldOfView = GetModifiedFov(_baseFov.Value);
                await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: cancellationToken);
            }
        }

        private void StopAdjustFov()
        {
            _adjustFovCts?.Cancel();
            _adjustFovCts?.Dispose();
            _adjustFovCts = null;
        }
        
        private static float GetModifiedFov(float fovDegree)
        {
            var windowAspect = Screen.width * 1f / Screen.height;

            if (windowAspect <= SpoutVideoAspect)
            {
                return fovDegree;
            }

            var result = Mathf.Rad2Deg * 2f * Mathf.Atan(Mathf.Tan(fovDegree * Mathf.Deg2Rad * 0.5f) * windowAspect / SpoutVideoAspect);
            return result;
        }
    }
}
