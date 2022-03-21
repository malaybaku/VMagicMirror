using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Baku.VMagicMirror.ViewModelsConfig
{
    /// <summary>
    /// アプリの起動時から終了時までアイテム名の一覧に追従したうえで、
    /// 選択肢の冒頭に「なし」の選択肢が追加されているようなViewModel.
    /// ComboBoxで使えるように作られている
    /// </summary>
    public class AccessoryItemNamesViewModel
    {
        internal AccessoryItemNamesViewModel(AccessorySettingModel model)
        {
            _model = model;
            Items = new ReadOnlyObservableCollection<AccessoryItemNameViewModel>(_items);
            _items.Add(AccessoryItemNameViewModel.None);
            OnItemRefreshed();

            _model.ItemUpdated += OnItemUpdated;
            _model.ItemNameMaybeChanged += OnItemUpdated;
            _model.ItemRefreshed += OnItemRefreshed;
            _model.ItemReloaded += OnItemRefreshed;            
        }

        private readonly AccessorySettingModel _model;

        private readonly ObservableCollection<AccessoryItemNameViewModel> _items
            = new ObservableCollection<AccessoryItemNameViewModel>();

        public ReadOnlyObservableCollection<AccessoryItemNameViewModel> Items { get; }

        private void OnItemRefreshed()
        {
            //TODO: 変化しないアイテムについてはインスタンスを維持しつつDisplayNameをあわせる
            //消えたり増えたアイテムに対しては素朴に要素を増減する

            var itemToRemove = new List<AccessoryItemNameViewModel>();
            var itemToAdd = new List<AccessoryItemSetting>();

            //既存要素の更新+足りないものチェック
            foreach (var item in _model.Items.Items)
            {
                if (_items.FirstOrDefault(i => i.FileId == item.FileId) is { } target)
                {
                    target.DisplayName.Value = item.Name;
                }
                else
                {
                    itemToAdd.Add(item);
                }
            }

            //余計なもの削除 + 足りないもの追加: 冒頭の空要素は削除しないことに注意
            foreach (var item in _items.Skip(1))
            {
                if (!_model.Items.Items.Any(i => i.FileId == item.FileId))
                {
                    itemToRemove.Add(item);
                }
            }

            foreach (var item in itemToRemove)
            {
                _items.Remove(item);
            }

            foreach (var item in itemToAdd)
            {
                _items.Add(new AccessoryItemNameViewModel(item.FileId, item.Name));
            }

            //ModelのItemsを正として並び順を揃える: 冒頭に空要素を置いているぶんインデックスがずれることに注意
            for(int i = 0; i < _model.Items.Items.Length; i++)
            {
                var fileId = _model.Items.Items[i].FileId;
                var item = _items.First(i => i.FileId == fileId);
                var currentIndex = _items.IndexOf(item);
                if (currentIndex != i + 1)
                {
                    _items.Move(currentIndex, i + 1);
                }
            }
        }

        private void OnItemUpdated(AccessoryItemSetting item)
        {
            if (_items.FirstOrDefault(i => i.FileId == item.FileId) is { } target)
            {
                target.DisplayName.Value = item.Name;
            }
        }
    }

    public class AccessoryItemNameViewModel : ViewModelBase
    {
        public AccessoryItemNameViewModel(string fileId, string displayName)
        {
            FileId = fileId;
            DisplayName.Value = displayName;
        }

        public string FileId { get; }

        public RProperty<string> DisplayName { get; } = new RProperty<string>("");

        public static AccessoryItemNameViewModel None { get; }
            = new AccessoryItemNameViewModel("", "(None)");
    }
}
