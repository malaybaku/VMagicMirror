using System.Linq;

namespace Baku.VMagicMirrorConfig
{
    internal class HotKeyActionRunner
    {
        public HotKeyActionRunner() : this(
            ModelResolver.Instance.Resolve<HotKeySettingModel>(),
            ModelResolver.Instance.Resolve<HotKeyModel>(),
            ModelResolver.Instance.Resolve<LayoutSettingModel>(),
            ModelResolver.Instance.Resolve<MotionSettingModel>(),
            ModelResolver.Instance.Resolve<WordToMotionSettingModel>(),
            ModelResolver.Instance.Resolve<AccessorySettingModel>()
            )
        {
        }

        public HotKeyActionRunner(
            HotKeySettingModel setting,
            HotKeyModel model,
            LayoutSettingModel layoutSetting,
            MotionSettingModel motionSetting,
            WordToMotionSettingModel wordToMotionSetting,
            AccessorySettingModel accessorySetting
            )
        {
            _setting = setting;
            _model = model;
            _layoutSetting = layoutSetting;
            _motionSetting = motionSetting;
            _wordToMotionSetting = wordToMotionSetting;
            _accessorySetting = accessorySetting;

            _model.ActionRequested += OnActionRequested;
        }

        private readonly HotKeyModel _model;
        private readonly HotKeySettingModel _setting;
        private readonly LayoutSettingModel _layoutSetting;
        private readonly MotionSettingModel _motionSetting;
        private readonly WordToMotionSettingModel _wordToMotionSetting;
        private readonly AccessorySettingModel _accessorySetting;

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
                case (int)HotKeyActionBodyMotionStyle.GameInputLoomotion:
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
