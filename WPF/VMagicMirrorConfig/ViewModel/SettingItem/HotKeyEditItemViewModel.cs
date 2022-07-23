using System;
using System.Text;
using System.Windows.Input;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    public class HotKeyEditItemViewModel : ViewModelBase
    {
        public HotKeyEditItemViewModel(HotKeyRegisterItem item)
        {
            _item = item;
            RegisteredKeyString.Value = CreateRegisteredKeyString();

            KeyDownCommand = new ActionCommand<object>(OnKeyDown);
            MoveUpCommand = new ActionCommand(() => MoveUpRequested?.Invoke(this));
            MoveDownCommand = new ActionCommand(() => MoveDownRequested?.Invoke(this));
            DeleteCommand = new ActionCommand(() => DeleteRequested?.Invoke(this));

            //KeyInputに何かの文字が入っても空にしちゃう。認識した文字については別UIとして同じ場所に表示する
            RegisteredKeyInput.PropertyChanged += (_, __) =>
            {
                if (!string.IsNullOrEmpty(RegisteredKeyInput.Value))
                {
                    RegisteredKeyInput.Value = "";
                }
            };

            ActionContent = new RProperty<HotKeyActionContent>(
                _item.ActionContent, OnActionContentChanged
                );
        }

        private HotKeyRegisterItem _item;

        public event Action<(HotKeyEditItemViewModel source, HotKeyRegisterItem item)>? UpdateItemRequested;
        public event Action<HotKeyEditItemViewModel>? MoveUpRequested;
        public event Action<HotKeyEditItemViewModel>? MoveDownRequested;
        public event Action<HotKeyEditItemViewModel>? DeleteRequested;

        public RProperty<string> RegisteredKeyInput { get; } = new RProperty<string>("");

        //"Ctrl + Shift + R"のような、ホットキーを示す表示専用の文字列が入る
        public RProperty<string> RegisteredKeyString { get; } = new RProperty<string>("");

        public RProperty<HotKeyActionContent> ActionContent { get; }

        public ActionCommand<object> KeyDownCommand { get; }

        public ActionCommand MoveUpCommand { get; }
        public ActionCommand MoveDownCommand { get; }
        public ActionCommand DeleteCommand { get; }


        private void OnKeyDown(object? obj)
        {
            if (obj is not Key key)
            {
                return;
            }

            var modifierKeys = Keyboard.Modifiers;
            if (_item.Key == key && _item.ModifierKeys == modifierKeys)
            {
                return;
            }

            var updated = _item with
            {
                Key = key,
                ModifierKeys = modifierKeys,
            };

            UpdateItemRequested?.Invoke((this, updated));
        }

        private void OnActionContentChanged(HotKeyActionContent actionContent)
        {
            if (_item.ActionContent == actionContent)
            {
                return;
            }

            _item = _item with
            {
                ActionContent = actionContent,
            };
            UpdateItemRequested?.Invoke((this, _item));
        }

        private string CreateRegisteredKeyString()
        {
            //NOTE: 制御キーを伴わないようなのは禁止しておく、word to motionと紛らわしいしホットキー的でないので
            if (_item.Key == Key.None || _item.ModifierKeys == ModifierKeys.None)
            {
                return "";
            }

            var sb = new StringBuilder();

            if (_item.ModifierKeys.HasFlag(ModifierKeys.Windows))
            {
                sb.Append("Win+");
            }

            if (_item.ModifierKeys.HasFlag(ModifierKeys.Control))
            {
                sb.Append("Ctrl+");
            }

            if (_item.ModifierKeys.HasFlag(ModifierKeys.Shift))
            {
                sb.Append("Shift+");
            }

            if (_item.ModifierKeys.HasFlag(ModifierKeys.Alt))
            {
                sb.Append("Alt+");
            }

            sb.Append(KeyToString(_item.Key));
            return sb.ToString();
        }

        private static string KeyToString(Key key)
        {
            if (key >= Key.D0 && key <= Key.D9)
            {
                return ((int)key - (int)Key.D0).ToString();
            }

            if (key >= Key.NumPad0 && key <= Key.NumPad9)
            {
                return "Num" + ((int)key - (int)Key.NumPad0).ToString();
            }

            return key.ToString();
        }
    }
}
