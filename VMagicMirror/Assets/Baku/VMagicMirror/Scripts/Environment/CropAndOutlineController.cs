using R3;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace Baku.VMagicMirror
{
    public class CropAndOutlineController : PresenterBase
    {
        private readonly IMessageReceiver _receiver;
        private readonly PostProcessVolume _postProcessVolume;

        private readonly ReactiveProperty<bool> _rawEnableCircleCrop = new(false);
        private readonly ReactiveProperty<bool> _windowFrameVisible = new(true);
        private readonly ReactiveProperty<bool> _enableFreeLayout = new(false);

        private readonly ReactiveProperty<bool> _enableCircleCrop = new(false);
        public ReadOnlyReactiveProperty<bool> EnableCircleCrop => _enableCircleCrop;

        private VmmCrop _vmmCrop;

        public CropAndOutlineController(
            IMessageReceiver receiver,
            PostProcessVolume postProcessVolume
            )
        {
            _receiver = receiver;
            _postProcessVolume = postProcessVolume;
        }
        
        public override void Initialize()
        {
            var vmmCrop = _postProcessVolume.profile.GetSetting<VmmCrop>();
            _vmmCrop = vmmCrop;
            
            // 透過中、かつフリーレイアウトがオフのときだけ切り抜く
            // (非透過で切り抜いても違和感ある + フリーレイアウト中に切り抜かれると操作が壊滅するため)
            _receiver.BindBoolProperty(VmmCommands.EnableCrop, _rawEnableCircleCrop);
            _receiver.BindBoolProperty(VmmCommands.WindowFrameVisibility, _windowFrameVisible);
            _receiver.BindBoolProperty(VmmCommands.EnableDeviceFreeLayout, _enableFreeLayout);

            _rawEnableCircleCrop
                .CombineLatest(
                    _windowFrameVisible,
                    _enableFreeLayout,
                    (circleCrop, frameVisible, freeLayout) => circleCrop && !frameVisible && !freeLayout
                )
                .DistinctUntilChanged()
                .Subscribe(enabled =>
                {
                    vmmCrop.active = enabled;
                    _enableCircleCrop.Value = enabled;
                })
                .AddTo(this);
            
            _receiver.AssignCommandHandler(
                VmmCommands.SetCropBorderColor,
                command =>
                {
                    var rgb = command.ToColorFloats();
                    vmmCrop.borderColor.value = new Color(rgb[0], rgb[1], rgb[2]);
                });
            
            _receiver.AssignCommandHandler(
                VmmCommands.SetCropSize,
                value => vmmCrop.margin.value = 1.0f - value.ToInt() * 0.001f
                );

            _receiver.AssignCommandHandler(
                VmmCommands.SetCropBorderWidth,
                value => vmmCrop.borderWidth.value = value.ToInt() * 0.001f
                );
            
            _receiver.AssignCommandHandler(
                VmmCommands.SetCropSquareRate,
                value => vmmCrop.squareRate.value = value.ToInt() * 0.01f
                );
        }

        public bool IsPointInsideCropArea(Vector2 mousePos)
        {
            var screenSize = (float) Mathf.Min(Screen.width, Screen.height);
            
            // diffのx,y座標はどちらも (-0.5, 0.5) の範囲になる
            var diff = (mousePos - new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)) / screenSize;
            
            // VmmCrop.shader でsdの符号を求めるのと同じ計算をすることで、mousePosが図形の内側にあるかどうか判定できる
            var margin = _vmmCrop.margin.value;
            var squareRate = _vmmCrop.squareRate.value;

            var halfSize = 0.5f * (1f - margin);
            var halfStraightLength = halfSize * squareRate;
            var radius = halfSize - halfStraightLength;

            var d = new Vector2(
                Mathf.Abs(diff.x) - halfStraightLength,
                Mathf.Abs(diff.y) - halfStraightLength
                );
            var sd =
                new Vector2(Mathf.Max(d.x, 0f), Mathf.Max(d.y, 0)).magnitude +
                Mathf.Min(Mathf.Max(d.x, d.y), 0f) -
                radius;
            return sd <= 0f;
        }
    }
}
