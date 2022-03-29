using Baku.VMagicMirrorConfig.View;

namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// ポインターの表示/非表示について責任を持つクラス
    /// </summary>
    class LargePointerVisibility
    {
        public LargePointerVisibility() : this(ModelResolver.Instance.Resolve<MotionSettingModel>())
        {
        }

        public LargePointerVisibility(MotionSettingModel motionSetting)
        {
            _motionSetting = motionSetting;
            _motionSetting.EnableNoHandTrackMode.PropertyChanged += (_, __) => UpdatePointerVisibility();
            _motionSetting.KeyboardAndMouseMotionMode.PropertyChanged += (_, __) => UpdatePointerVisibility();
            _motionSetting.ShowPresentationPointer.PropertyChanged += (_, __) => UpdatePointerVisibility();
            UpdatePointerVisibility();
        }

        private readonly MotionSettingModel _motionSetting;

        private void UpdatePointerVisibility() => LargePointerController.Instance.UpdateVisibility(            
            !_motionSetting.EnableNoHandTrackMode.Value && 
            _motionSetting.KeyboardAndMouseMotionMode.Value == MotionSetting.KeyboardMouseMotionPresentation &&
            _motionSetting.ShowPresentationPointer.Value
            );

    }
}
