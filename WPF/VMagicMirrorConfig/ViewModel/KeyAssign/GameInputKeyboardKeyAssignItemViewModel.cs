using System;
using System.Linq;
using System.Windows.Input;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    public class GameInputKeyAssignItemViewModel
    {
        public GameInputKeyAssignItemViewModel(GameInputButtonAction action, string key)
        {
            _actionItem = GameInputButtonActionItemViewModel.AvailableItems.FirstOrDefault(item => item.Action == action)
                ?? new GameInputButtonActionItemViewModel(GameInputButtonAction.None, "");

            KeyDownCommand = new ActionCommand<object>(OnKeyDown);
            ClearInputCommand = new ActionCommand(ClearInput);
            SetKey(key);

            //入力文字列は常にカラに戻す。入力欄は単にキー入力を受けるためだけに使われる
            RegisteredKeyInput.PropertyChanged += (_, __) =>
            {
                RegisteredKeyInput.Value = "";
            };
        }

        //NOTE: nullになるのはアクションにキーが割当たってない状態
        private Key? _key;

        private readonly GameInputButtonActionItemViewModel _actionItem;
        public GameInputButtonAction Action => _actionItem.Action;
        public RProperty<string> Label => _actionItem.Label;

        public RProperty<string> RegisteredKeyInput { get; } = new RProperty<string>("");
        public RProperty<string> RegisteredKey { get; } = new RProperty<string>("");

        public ActionCommand<object> KeyDownCommand { get; }
        public ActionCommand ClearInputCommand { get; }

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
            if (key is Key.None or Key.Delete or Key.Back or Key.LeftAlt or Key.RightAlt)
            {
                nextKey = null;
            }

            if (_key == nextKey)
            {
                return;
            }

            _key = nextKey;
            RegisteredKey.Value = CreateRegisteredKeyString();
            RegisteredKeyChanged?.Invoke(_key?.ToString() ?? "");
        }

        //NOTE: 横着でこう書いてるが、別にOnKeyDownに帰着させないでも動くならOK
        private void ClearInput() => OnKeyDown(Key.None);

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

            return key.ToString().ToUpper();
        }
    }
}
