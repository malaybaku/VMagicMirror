using System;
using System.Windows.Input;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    public class GameInputKeyAssignItemViewModel
    {
        public GameInputKeyAssignItemViewModel(GameInputActionKey actionKey, string keyCode)
        {
            ActionKey = actionKey;
            KeyDownCommand = new ActionCommand<object>(OnKeyDown);
            ClearInputCommand = new ActionCommand(ClearInput);
            SetKey(keyCode);

            //入力文字列は常にカラに戻す。入力欄は単にキー入力を受けるためだけに使われる
            RegisteredKeyInput.PropertyChanged += (_, __) =>
            {
                RegisteredKeyInput.Value = "";
            };
        }

        //NOTE: nullになるのはアクションにキーが割当たってない状態
        private Key? _key;

        public GameInputActionKey ActionKey { get; }

        public RProperty<string> RegisteredKeyInput { get; } = new RProperty<string>("");
        public RProperty<string> RegisteredKey { get; } = new RProperty<string>("");

        public ActionCommand<object> KeyDownCommand { get; }
        public ActionCommand ClearInputCommand { get; }

        public event Action<(GameInputActionKey key, string keyCode)>? RegisteredKeyChanged;

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
            if (key is Key.None or Key.Delete or Key.Back)
            {
                nextKey = null;
            }

            if (_key == nextKey)
            {
                return;
            }

            //NOTE: (Left|Right)(Alt|Ctrl|Shift|Win)が入る事がある。はず。
            _key = nextKey;
            RegisteredKey.Value = CreateRegisteredKeyString();
            RegisteredKeyChanged?.Invoke((ActionKey, _key?.ToString() ?? ""));
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

            //アルファベットのキーは大文字にするが、それ以外はそのまま
            var value = key.ToString();
            return value.Length == 1 ? value.ToUpper() : value;
        }
    }
}
