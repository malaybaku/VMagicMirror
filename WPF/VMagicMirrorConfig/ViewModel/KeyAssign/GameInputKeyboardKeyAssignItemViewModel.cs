using System;
using System.Linq;
using System.Windows.Input;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    public class GameInputKeyAssingInputViewModel
    {
        public GameInputKeyAssingInputViewModel(GameInputButtonAction action, string key)
        {
            _actionItem = GameInputButtonActionItemViewModel.AvailableItems.FirstOrDefault(item => item.Action == action)
                ?? new GameInputButtonActionItemViewModel(GameInputButtonAction.None, "");

            KeyDownCommand = new ActionCommand<object>(OnKeyDown);
            SetKey(key);
        }

        //NOTE: nullになるのはアクションにキーが割当たってない状態
        private Key? _key;

        private readonly GameInputButtonActionItemViewModel _actionItem;
        public GameInputButtonAction Action => _actionItem.Action;
        public RProperty<string> Label => _actionItem.Label;

        public RProperty<string> RegisteredKey { get; } = new RProperty<string>("");

        public ActionCommand<object> KeyDownCommand { get; }

        public event Action<string>? RegisteredKeyChanged;

        public void SetKey(string key)
        {
            Key? nextKey = Enum.TryParse<Key>(key, out var result) ? result : null;
            if (_key == nextKey)
            {
                return;
            }

            _key = nextKey;
            RegisteredKey.Value = CreateRegisteredKeyString();
        }

        private void OnKeyDown(object? obj)
        {
            if (obj is not Key key)
            {
                return;
            }

            Key? nextKey = key;
            if (key == Key.Delete || key == Key.Back)
            {
                nextKey = null;
            }

            if (_key == nextKey)
            {
                return;
            }

            _key = key;
            RegisteredKey.Value = CreateRegisteredKeyString();
            RegisteredKeyChanged?.Invoke(_key?.ToString() ?? "");
        }

        private string CreateRegisteredKeyString()
        {
            if (_key == null)
            {
                return "";
            }

            var key = _key.Value;

            //ラクにケアできる範囲で、数字キーのみ区別する
            if (key >= Key.NumPad0 && key <= Key.NumPad9)
            {
                return "Num" + ((int)key - (int)Key.NumPad0).ToString();
            }

            return new string(new char[] { KeyToStringUtil.GetCharFromKey(key) });
        }
    }
}
