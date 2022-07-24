using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;

namespace Baku.VMagicMirrorConfig
{
    class HotKeySettingModel : SettingModelBase<HotKeySetting>
    {
        public HotKeySettingModel() : this(ModelResolver.Instance.Resolve<IMessageSender>())
        {
        }

        public HotKeySettingModel(IMessageSender sender) : base(sender)
        {
            Items = new ReadOnlyObservableCollection<HotKeyRegisterItem>(_items);
            InvalidItems = new ReadOnlyObservableCollection<HotKeyRegisterItem>(_invalidItems);

            var setting = HotKeySetting.Default;
            EnableHotKey = new RProperty<bool>(setting.EnableHotKey);
            LoadSerializedItems(setting);            
        }

        protected override void PreSave()
        {
            SaveToSerializedItems();
        }

        protected override void AfterLoad(HotKeySetting entity)
        {
            LoadSerializedItems(entity);
            RaiseUpdated();
        }

        //MoveUp/MoveDown/Delete/AddNewItemを呼び出した場合はUpdateではなくコッチが発火する
        public event Action? SingleItemUpdated;
        private void RaiseSingleItemUpdated() => SingleItemUpdated?.Invoke();

        public event EventHandler<EventArgs>? Updated;
        private void RaiseUpdated() => Updated?.Invoke(this, EventArgs.Empty);

        public RProperty<bool> EnableHotKey { get; }
        public RProperty<string> SerializedItems { get; } = new RProperty<string>("");

        private readonly ObservableCollection<HotKeyRegisterItem> _items
            = new ObservableCollection<HotKeyRegisterItem>();
        public ReadOnlyObservableCollection<HotKeyRegisterItem> Items { get; }

        private readonly ObservableCollection<HotKeyRegisterItem> _invalidItems = new();
        public ReadOnlyObservableCollection<HotKeyRegisterItem> InvalidItems { get; }

        public void SetItem(int index, HotKeyRegisterItem item)
        {
            if (index >= 0 && index < Items.Count && !_items[index].Equals(item))
            {
                _items[index] = item;
                RaiseSingleItemUpdated();
            }
        }

        public override void ResetToDefault()
        {
            _items.Clear();
            RaiseUpdated();

            var defaultSetting = DefaultHotKeySetting.Load();
            foreach (var item in defaultSetting)
            {
                _items.Add(item);
            }
            RaiseUpdated();
        }

        internal void MoveUp(int index)
        {
            if (index > 0 && index < _items.Count)
            {
                _items.Move(index, index - 1);
                RaiseSingleItemUpdated();
            }
        }

        internal void MoveDown(int index)
        {
            if (index >= 0 && index < _items.Count - 1)
            {
                _items.Move(index, index + 1);
                RaiseSingleItemUpdated();
            }
        }

        internal void Delete(int index)
        {
            if (index >= 0 && index < _items.Count)
            {
                _items.RemoveAt(index);
                RaiseSingleItemUpdated();
            }
        }

        internal void AddNewItem()
        {
            //NOTE: 空じゃないアクションを指定した状態にする…手もあるが、一旦無しで
            _items.Add(HotKeyRegisterItem.Empty());
            RaiseSingleItemUpdated();
        }

        /// <summary>
        /// HotKeySetterから呼び出すことで、登録できなかったアイテムを追加する
        /// </summary>
        /// <param name="item"></param>
        internal void AddInvalidItem(HotKeyRegisterItem item)
        {
            if (!_invalidItems.Contains(item))
            {
                _invalidItems.Add(item);
            }
        }

        /// <summary>
        /// HotKeySetterから呼び出すことで、無効なアイテムとして考慮しなくても良くなったものを削除する
        /// </summary>
        /// <param name="item"></param>
        internal void RemoveInvalidItem(HotKeyRegisterItem item)
        {
            if (_invalidItems.Contains(item))
            {
                _invalidItems.Remove(item);
            }
        }

        private void LoadSerializedItems(HotKeySetting setting)
        {
            _items.Clear();

            // 文字列が空の場合、設定ファイルがなかったと推定する。
            // ユーザーが明示的にカラにした場合は"{}"的な何かになるので
            if (string.IsNullOrEmpty(setting.SerializedItems))
            {
                ResetToDefault();
                return;
            }

            try
            {
                var collection = JsonSerializer.Deserialize<HotKeySettingItemCollection>(setting.SerializedItems);
                if (collection == null)
                {
                    return;
                }

                foreach (var item in collection.Items)
                {
                    _items.Add(new HotKeyRegisterItem(
                        (ModifierKeys)item.ModifierKeys,
                        (Key)item.Key,
                        new HotKeyActionContent((HotKeyActions)item.Action, item.ActionArgNumber)
                        ));
                }
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(SerializedItems.Value))
                {
                    LogOutput.Instance.Write(ex);
                }
            }
        }

        private void SaveToSerializedItems()
        {
            var items = Items
                .Select(i => new HotKeySettingItem()
                {
                    Action = (int)i.ActionContent.Action,
                    ActionArgNumber = i.ActionContent.ArgNumber,
                    Key = (int)i.Key,
                    ModifierKeys = (int)i.ModifierKeys,
                })
                .ToArray();

            SerializedItems.Value = JsonSerializer.Serialize(
                new HotKeySettingItemCollection()
                {
                    Items = items,
                });
        }

    }
}
