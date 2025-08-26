using System.Linq;

namespace Baku.VMagicMirrorConfig
{
    internal class HotKeyActionRunner
    {
        public HotKeyActionRunner() : this(
            ModelResolver.Instance.Resolve<HotKeySettingModel>(),
            ModelResolver.Instance.Resolve<HotKeyModel>(),
            ModelResolver.Instance.Resolve<LayoutSettingModel>(),
            ModelResolver.Instance.Resolve<LightSettingModel>(),
            ModelResolver.Instance.Resolve<MotionSettingModel>(),
            ModelResolver.Instance.Resolve<WordToMotionSettingModel>(),
            ModelResolver.Instance.Resolve<AccessorySettingModel>(),
            ModelResolver.Instance.Resolve<GamepadSettingModel>(),
            ModelResolver.Instance.Resolve<VMCPSettingModel>()
            )
        {
        }

        public HotKeyActionRunner(
            HotKeySettingModel setting,
            HotKeyModel model,
            LayoutSettingModel layoutSetting,
            LightSettingModel lightSetting,
            MotionSettingModel motionSetting,
            WordToMotionSettingModel wordToMotionSetting,
            AccessorySettingModel accessorySetting,
            GamepadSettingModel gamepadSetting,
            VMCPSettingModel vmcpSetting
            )
        {
            _setting = setting;
            _model = model;
            _layoutSetting = layoutSetting;
            _lightSetting = lightSetting;
            _motionSetting = motionSetting;
            _wordToMotionSetting = wordToMotionSetting;
            _accessorySetting = accessorySetting;
            _gamepadSetting = gamepadSetting;
            _vmcpSetting = vmcpSetting;

            _model.ActionRequested += OnActionRequested;
        }

        private readonly HotKeyModel _model;
        private readonly HotKeySettingModel _setting;
        private readonly LayoutSettingModel _layoutSetting;
        private readonly LightSettingModel _lightSetting;
        private readonly MotionSettingModel _motionSetting;
        private readonly WordToMotionSettingModel _wordToMotionSetting;
        private readonly AccessorySettingModel _accessorySetting;
        private readonly VMCPSettingModel _vmcpSetting;
        private readonly GamepadSettingModel _gamepadSetting;

        private void OnActionRequested(HotKeyActionContent content)
        {
            if (!_setting.EnableHotKey.Value)
            {
                return;
            }

            //NOTE: 表示上はSetCameraもWordToMotionもindexが1から始まる想定
            switch (content.Action)
            {
                case HotKeyActions.SetCamera:
                    _layoutSetting.QuickLoadViewPoint(content.ArgNumber);
                    break;
                case HotKeyActions.SetBodyMotionStyle:
                    SetMotionStyle(content.ArgNumber);
                    break;
                case HotKeyActions.CallWtm:
                    _wordToMotionSetting.Play(content.ArgNumber - 1);
                    break;
                case HotKeyActions.ToggleAccessory:
                    ToggleAccessoryVisibility(content.ArgString);
                    break;
                case HotKeyActions.ToggleVMCPReceiveActive:
                    _vmcpSetting.VMCPEnabled.Value = !_vmcpSetting.VMCPEnabled.Value;
                    break;
                case HotKeyActions.ToggleKeyboardVisibility:
                    _layoutSetting.HidVisibility.Value = !_layoutSetting.HidVisibility.Value;
                    break;
                case HotKeyActions.TogglePenVisibility:
                    _layoutSetting.PenVisibility.Value = !_layoutSetting.PenVisibility.Value;
                    break;
                case HotKeyActions.ToggleGamepadVisibility:
                    _gamepadSetting.GamepadVisibility.Value = !_gamepadSetting.GamepadVisibility.Value;
                    break;
                case HotKeyActions.ToggleShadowVisibility:
                    _lightSetting.EnableShadow.Value = !_lightSetting.EnableShadow.Value;
                    break;
                case HotKeyActions.ToggleOutlineVisibility:
                    _lightSetting.EnableOutlineEffect.Value = !_lightSetting.EnableOutlineEffect.Value;
                    break;
                case HotKeyActions.ToggleWindVisibility:
                    _lightSetting.EnableWind.Value = !_lightSetting.EnableWind.Value;
                    break;
                case HotKeyActions.EnableHandTracking:
                    _motionSetting.EnableImageBasedHandTracking.Value = true;
                    break;
                case HotKeyActions.DisableHandTracking:
                    _motionSetting.EnableImageBasedHandTracking.Value = false;
                    break;
                case HotKeyActions.ToggleHandTracking:
                    _motionSetting.EnableImageBasedHandTracking.Value = !_motionSetting.EnableImageBasedHandTracking.Value;
                    break;
                case HotKeyActions.SetKeyMouseMotion:
                    _motionSetting.KeyboardAndMouseMotionMode.Value = (HotKeyActionKeyMouseMotionStyle)content.ArgNumber switch
                    {
                        HotKeyActionKeyMouseMotionStyle.Default => MotionSetting.KeyboardMouseMotionDefault,
                        HotKeyActionKeyMouseMotionStyle.PresentationMode => MotionSetting.KeyboardMouseMotionPresentation,
                        HotKeyActionKeyMouseMotionStyle.Tablet => MotionSetting.KeyboardMouseMotionPenTablet,
                        HotKeyActionKeyMouseMotionStyle.None => MotionSetting.KeyboardMouseMotionNone,
                        _ => MotionSetting.KeyboardMouseMotionDefault,
                    };
                    break;
                case HotKeyActions.None:
                default:
                    //何もしない
                    break;
            }
        }

        private void ToggleAccessoryVisibility(string fileId)
        {
            var item = _accessorySetting.Items.Items.FirstOrDefault(i => i.FileId == fileId);
            if (item == null)
            {
                return;
            }

            item.IsVisible = !item.IsVisible;
            _accessorySetting.UpdateItemFromUi(item);
        }

        private void SetMotionStyle(int style)
        {
            switch (style)
            {
                case (int)HotKeyActionBodyMotionStyle.Default:
                    _motionSetting.EnableNoHandTrackMode.Value = false;
                    _motionSetting.EnableGameInputLocomotionMode.Value = false;
                    break;
                case (int)HotKeyActionBodyMotionStyle.AlwaysHandDown:
                    _motionSetting.EnableNoHandTrackMode.Value = true;
                    _motionSetting.EnableGameInputLocomotionMode.Value = false;
                    break;
                case (int)HotKeyActionBodyMotionStyle.GameInputLocomotion:
                    _motionSetting.EnableNoHandTrackMode.Value = false;
                    _motionSetting.EnableGameInputLocomotionMode.Value = true;
                    break;
                default:
                    //未知: 何もしない(将来verの設定ファイルを読むと起こりうる)
                    break;
            }
        }

    }
}
