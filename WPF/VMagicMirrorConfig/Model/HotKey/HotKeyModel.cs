using System;
using System.Collections.Generic;
using System.Windows;
using System.Linq;

namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// ホットキーを監視するすごいやつだよ
    /// </summary>
    class HotKeyModel
    {
        private HotKeyWrapper? _hotKeyWrapper;

        private readonly List<HotKeyRegisterItem> _queuedItems = new List<HotKeyRegisterItem>();
        private readonly Dictionary<int, HotKeyRegisterItem> _registeredActions = new Dictionary<int, HotKeyRegisterItem>();

        public IEnumerable<HotKeyRegisterItem> LoadRegisteredItems() => _registeredActions.Values;
        public event Action? RegisteredItemsChanged;
        public event Action<HotKeyActionContent>? ActionRequested;

        public HotKeyModel()
        {
            if (Application.Current is App app)
            {
                app.MainWindowInitialized += OnMainWindowInitialized;
            }
        }

        private void OnMainWindowInitialized()
        {
            if (Application.Current is App app)
            {
                app.MainWindowInitialized -= OnMainWindowInitialized;
            }

            _hotKeyWrapper = new HotKeyWrapper(Application.Current.MainWindow);
            _hotKeyWrapper.HotKeyActionRequested += OnActionRequested;
            foreach(var item in _queuedItems)
            {
                Register(item);
            }
            _queuedItems.Clear();
        }

        public void Register(HotKeyRegisterItem item)
        {
            if (_hotKeyWrapper == null)
            {
                //NOTE: ここで重複回避してもいいし、しなくてもよい
                _queuedItems.Add(item);
                return;
            }

            if (_registeredActions.Values.Contains(item))
            {
                //登録済みのものなので何もしない
                //TODO: UIの仕組みによっては単純なガードではなくRefCount的なのが欲しくなるかも
                return;
            }

            var id = _hotKeyWrapper.Register(item);
            if (id >= 0)
            {
                _registeredActions[id] = item;
                RegisteredItemsChanged?.Invoke();
            }
            else
            {
                LogOutput.Instance.Write($"Tried to register hot key, but failed. key={item.Key}, mod={item.ModifierKeys}");
            }
        }

        public void Unregister(HotKeyRegisterItem item)
        {
            if (_hotKeyWrapper == null)
            {
                //NOTE: 起動直後に呼んだUnregisterは基本的に無視してもよいはずなので無視
                return;
            }

            var pair = _registeredActions.FirstOrDefault(p => p.Key.Equals(item));
            if (pair.Value != null)
            {
                _hotKeyWrapper.Unregister(pair.Key);
                _registeredActions.Remove(pair.Key);
                RegisteredItemsChanged?.Invoke();
            }
        }

        private void OnActionRequested(HotKeyActionContent content)
        {
            Application.Current.Dispatcher.BeginInvoke(
                new Action(() => ActionRequested?.Invoke(content))
            );
        }
    }
}
