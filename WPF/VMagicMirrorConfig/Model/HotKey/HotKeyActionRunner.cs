namespace Baku.VMagicMirrorConfig.Model.HotKey
{
    internal class HotKeyActionRunner
    {
        public HotKeyActionRunner() : this(
            ModelResolver.Instance.Resolve<HotKeyWrapper>(),
            ModelResolver.Instance.Resolve<HotKeySettingModel>(),
            ModelResolver.Instance.Resolve<LayoutSettingModel>(),
            ModelResolver.Instance.Resolve<WordToMotionSettingModel>()
            )
        {
        }

        public HotKeyActionRunner(
            HotKeyWrapper eventSource, 
            HotKeySettingModel setting, 
            LayoutSettingModel layoutSetting,
            WordToMotionSettingModel wordToMotionSetting
            )
        {
            _eventSource = eventSource;
            _setting = setting;
            _layoutSetting = layoutSetting;
            _wordToMotionSetting = wordToMotionSetting;

            _eventSource.HotKeyActionRequested += OnActionRequested;
        }

        private HotKeyWrapper _eventSource;
        private HotKeySettingModel _setting;
        private LayoutSettingModel _layoutSetting;
        private WordToMotionSettingModel _wordToMotionSetting;

        private void OnActionRequested(HotKeyActionContent content)
        {
            if (!_setting.EnableHotKey.Value)
            {
                return;
            }

            switch (content.Action)
            {
                case HotKeyActions.SetCamera:
                    _layoutSetting.QuickLoadViewPoint(content.ArgNumber);
                    break;
                case HotKeyActions.CallWtm:
                    _wordToMotionSetting.Play(content.ArgNumber);
                    break;
                case HotKeyActions.None:
                default:
                    //何もしない
                    break;
            }
        }
    }
}
