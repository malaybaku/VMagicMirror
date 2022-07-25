using System.Windows.Input;

namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// UIには特に何も出さず、モックで決め打ちのホットキーセットを登録してくれるすごいやつだよ
    /// </summary>
    class MockHotKeySetter
    {
        public MockHotKeySetter() : this(ModelResolver.Instance.Resolve<HotKeyModel>())
        {

        }

        public MockHotKeySetter(HotKeyModel model)
        {
            _model = model;
            SetMockHotKeys();
        }

        private readonly HotKeyModel _model;

        private void SetMockHotKeys()
        {
            LogOutput.Instance.Write("set mock hot keys");

            _model.Register(new HotKeyRegisterItem(
                ModifierKeys.Alt, Key.Q, new HotKeyActionContent(HotKeyActions.SetCamera, 1, ""))
                );
            _model.Register(new HotKeyRegisterItem(
                ModifierKeys.Alt, Key.W, new HotKeyActionContent(HotKeyActions.SetCamera, 2, ""))
                );
            _model.Register(new HotKeyRegisterItem(
                ModifierKeys.Alt, Key.E, new HotKeyActionContent(HotKeyActions.SetCamera, 3, ""))
                );

            for (var i = 0; i < 9; i++)
            {
                _model.Register(new HotKeyRegisterItem(
                    ModifierKeys.Alt, Key.D1 + i, new HotKeyActionContent(HotKeyActions.CallWtm, i, "")
                    ));
            }
        }
    }
}
