using System.Collections;
using System.Collections.Generic;
using Baku.VMagicMirror;
using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public class CameraFovController : PresenterBase
    {
        private readonly Camera _mainCamera;
        private readonly IMessageReceiver _receiver;
        private readonly SpoutSenderController _spoutSenderController;

        private readonly ReactiveProperty<float> _baseFov = new ReactiveProperty<float>(40f);

        public CameraFovController(
            Camera mainCamera,
            IMessageReceiver receiver, 
            SpoutSenderController spoutSenderController)
        {
            _mainCamera = mainCamera;
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

            _baseFov.CombineLatest(
                    _spoutSenderController.NeedFovModify,
                    (fov, needModify) => (fov, needModify)
                )
                .Subscribe(v =>
                {
                    var (fov, needModify) = v;
                    if (!needModify)
                    {
                        _mainCamera.fieldOfView = fov;
                        return;
                    }

                    var modified = GetModifiedFov(fov, Screen.width * 1f / Screen.height);
                    _mainCamera.fieldOfView = modified;
                })
                .AddTo(this);
        }

        private float GetModifiedFov(float fovDegree, float windowAspect)
        {
            //NOTE: Spoutの固定解像度のアス比が固定である前提で引数無しにしてる
            const float SpoutAspect = 16f / 9f;

            if (windowAspect <= SpoutAspect)
            {
                return fovDegree;
            }

            return Mathf.Rad2Deg * Mathf.Atan(Mathf.Tan(fovDegree * Mathf.Deg2Rad) * windowAspect / SpoutAspect);
        }
    }
}
