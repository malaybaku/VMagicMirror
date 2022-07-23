using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;

namespace Baku.VMagicMirrorConfig
{
    //TODO: この設定は他の設定と違うファイルに永続化したい。設定ファイルが変わったからって変わるようなものでもないので
    class HotKeySettingModel : SettingModelBase<HotKeySetting>
    {
        public HotKeySettingModel() : this(ModelResolver.Instance.Resolve<IMessageSender>())
        {
        }

        public HotKeySettingModel(IMessageSender sender) : base(sender)
        {
            Items = new ReadOnlyObservableCollection<HotKeyRegisterItem>(_items);

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

        public void SetItem(int index, HotKeyRegisterItem item)
        {
            if (index >= 0 && index < Items.Count && !_items[index].Equals(item))
            {
                _items[index] = item;
                RaiseUpdated();
            }
        }

        public override void ResetToDefault()
        {
            _items.Clear();
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

        private void LoadSerializedItems(HotKeySetting setting)
        {
            _items.Clear();

            // 空であることと初期設定が入ってることを等価に取り扱う
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
