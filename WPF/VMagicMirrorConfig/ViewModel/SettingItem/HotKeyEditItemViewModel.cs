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

        //TODO: フォーカス制御の関係で、ActionContentが差し替わったときにUIが再生成されるのは避けたいのでは？
        //NOTE: _itemが変わるとViewModelは破棄して再生成されるため、ここはreadonlyでok
        private readonly HotKeyRegisterItem _item;

        public event Action<(HotKeyEditItemViewModel source, HotKeyRegisterItem item)>? UpdateItemRequested;

        public RProperty<string> RegisteredKeyInput { get; } = new RProperty<string>("");

        //"Ctrl + Shift + R"のような、ホットキーを示す表示専用の文字列が入る
        public RProperty<string> RegisteredKeyString { get; } = new RProperty<string>("");

        public RProperty<HotKeyActionContent> ActionContent { get; }

        public ActionCommand<object> KeyDownCommand { get; }

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

            var item = _item with
            {
                ActionContent = actionContent,
            };
            UpdateItemRequested?.Invoke((this, item));
        }

        private string CreateRegisteredKeyString()
        {
            //NOTE: ホットキー無しの単発キーも禁止ということにしておく。分かりやすいので
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

            sb.Append(_item.Key.ToString());
            return sb.ToString();
        }
    }
}
