using System;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    public class HotKeyEditItemViewModel : ViewModelBase
    {
        private const string ResourcePrefix = "Hotkey_Action_";

        public HotKeyEditItemViewModel(HotKeyRegisterItem item)
        {
            _item = item;
            RegisteredKeyString.Value = CreateRegisteredKeyString();

            //KeyInputに何かの文字が入っても空にしちゃう。認識した文字については別UIとして同じ場所に表示する
            RegisteredKeyInput.PropertyChanged += (_, __) =>
            {
                if (!string.IsNullOrEmpty(RegisteredKeyInput.Value))
                {
                    RegisteredKeyInput.Value = "";
                }
            };

            UpdateActionDisplayName();
            WeakEventManager<LanguageSelector, PropertyChangedEventArgs>.AddHandler(
                LanguageSelector.Instance, nameof(PropertyChanged), OnLanguageChanged
                );
        }

        private void OnLanguageChanged(object? sender, PropertyChangedEventArgs e)
        {
            UpdateActionDisplayName();
        }

        //NOTE: _itemが変わるとViewModelは破棄して再生成されるため、ここはreadonlyでok
        private readonly HotKeyRegisterItem _item;

        public event Action<(HotKeyEditItemViewModel source, HotKeyRegisterItem item)>? UpdateItemRequested;

        public RProperty<string> RegisteredKeyInput { get; } = new RProperty<string>("");

        //"Ctrl + Shift + R"のような、ホットキーを示す表示専用の文字列が入る
        public RProperty<string> RegisteredKeyString { get; } = new RProperty<string>("");

        public RProperty<string> ActionDisplayName { get; } = new RProperty<string>("");

        private ActionCommand<object>? _keyDownCommand;
        public ActionCommand<object> KeyDownCommand
            => _keyDownCommand ??= new ActionCommand<object>(OnKeyDown);

        private void OnKeyDown(object? obj)
        {
            if (obj is not Key key)
            {
                return;
            }

            DetectHotKey(key);
        }

        private void DetectHotKey(Key key)
        {
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

        private void UpdateActionDisplayName()
        {
            ActionDisplayName.Value = _item.ActionContent.Action switch
            {
                HotKeyActions.None => GetLocalizedString("None"),
                HotKeyActions.SetCamera => string.Format(
                    GetLocalizedString("SetCamera_Format"), _item.ActionContent.ArgNumber),
                HotKeyActions.CallWtm => string.Format(
                    GetLocalizedString("CallWtm_Format"), _item.ActionContent.ArgNumber),
                //来ないハズ
                _ => GetLocalizedString("None"),
            };
        }
        
        private static string GetLocalizedString(string suffix)
            => LocalizedString.GetString(ResourcePrefix + suffix);
    }
}
