using R3;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace Baku.VMagicMirror
{
    public class WindowCropPresenter : PresenterBase
    {
        private readonly IMessageReceiver _receiver;
        private readonly PostProcessVolume _postProcessVolume;

        private readonly ReactiveProperty<bool> _enableCircleCrop = new(false);
        private readonly ReactiveProperty<bool> _windowFrameVisible = new(true);
        private readonly ReactiveProperty<bool> _enableFreeLayout = new(false);
        private readonly ReactiveProperty<Color> _cropBorderColor = new(Color.white);
        
        public WindowCropPresenter(
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
            
            // 透過中、かつフリーレイアウトがオフのときだけ切り抜く
            // (非透過で切り抜いても違和感ある + フリーレイアウト中に切り抜かれると操作が壊滅するため)
            _receiver.BindBoolProperty(VmmCommands.EnableCircleCrop, _enableCircleCrop);
            _receiver.BindBoolProperty(VmmCommands.WindowFrameVisibility, _windowFrameVisible);
            _receiver.BindBoolProperty(VmmCommands.EnableDeviceFreeLayout, _enableFreeLayout);

            _enableCircleCrop
                .CombineLatest(
                    _windowFrameVisible,
                    _enableFreeLayout,
                    (circleCrop, frameVisible, freeLayout) => circleCrop && !frameVisible && !freeLayout
                )
                .DistinctUntilChanged()
                .Subscribe(enabled => vmmCrop.active = enabled)
                .AddTo(this);
            
            _receiver.AssignCommandHandler(
                VmmCommands.SetCircleCropBorderColor,
                command =>
                {
                    var rgb = command.ToColorFloats();
                    vmmCrop.borderColor.value = new Color(rgb[0], rgb[1], rgb[2]);
                });
            
            _receiver.AssignCommandHandler(
                VmmCommands.SetCircleCropSize,
                value => vmmCrop.margin.value = 1.0f - value.ToInt() * 0.001f
                );

            _receiver.AssignCommandHandler(
                VmmCommands.SetCircleCropBorderWidth,
                value => vmmCrop.borderWidth.value = value.ToInt() * 0.001f
                );
        }
    }
}
