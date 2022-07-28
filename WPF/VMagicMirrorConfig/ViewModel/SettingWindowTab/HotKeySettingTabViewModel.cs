using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    public class HotKeySettingTabViewModel : ViewModelBase
    {
        public HotKeySettingTabViewModel() : this(ModelResolver.Instance.Resolve<HotKeySettingModel>())
        {
        }

        internal HotKeySettingTabViewModel(HotKeySettingModel model)
        {
            _model = model;

            if (!IsInDesignMode)
            {
                WeakEventManager<HotKeySettingModel, EventArgs>.AddHandler(model, nameof(model.Updated), OnModelItemUpdated);
                //NOTE: SingleItemUpdatedはViewModelでは監視しない
                RefreshItems();
            }
        }

        private readonly HotKeySettingModel _model;

        public RProperty<bool> EnableHotKey => _model.EnableHotKey;

        public ObservableCollection<HotKeyEditItemViewModel> Items { get; }
            = new ObservableCollection<HotKeyEditItemViewModel>();
        
        private ActionCommand? _addNewItemCommand;
        public ActionCommand AddNewItemCommand
            => _addNewItemCommand ??= new ActionCommand(AddNewItem);

        private ActionCommand? _resetCommand;
        public ActionCommand ResetCommand => _resetCommand ??= new ActionCommand(ResetToDefault);

        private void RefreshItems()
        {
            foreach (var item in Items)
            {
                UnsubscribeItem(item);
            }
            Items.Clear();

            foreach (var item in _model.Items)
            {
                var vm = new HotKeyEditItemViewModel(item);
                AddItemViewModel(vm);
            }
        }

        private void MoveUpItem(HotKeyEditItemViewModel item)
        {
            var index = Items.IndexOf(item);
            if (index > 0)
            {
                Items.Move(index, index - 1);
                _model.MoveUp(index);
            }
        }

        private void MoveDownItem(HotKeyEditItemViewModel item)
        {
            var index = Items.IndexOf(item);
            if (index >= 0 && index < Items.Count - 1)
            {
                Items.Move(index, index + 1);
                _model.MoveDown(index);
            }
        }

        private void DeleteItem(HotKeyEditItemViewModel item)
        {
            var index = Items.IndexOf(item);
            if (index >= 0 && index < Items.Count)
            {
                item.UpdateItemRequested -= OnUpdateItemRequested;
                item.MoveUpRequested -= MoveUpItem;
                item.MoveDownRequested -= MoveDownItem;
                item.DeleteRequested -= DeleteItem;
                Items.RemoveAt(index);
                _model.Delete(index);
            }
        }

        private void AddNewItem()
        {
            _model.AddNewItem();
            var vm = new HotKeyEditItemViewModel(_model.Items[^1]);
            AddItemViewModel(vm);
        }

        private void AddItemViewModel(HotKeyEditItemViewModel item)
        {
            item.UpdateItemRequested += OnUpdateItemRequested;
            item.MoveUpRequested += MoveUpItem;
            item.MoveDownRequested += MoveDownItem;
            item.DeleteRequested += DeleteItem;
            item.SubscribeInvalidItemSource(_model.InvalidItems);
            Items.Add(item);
        }

        private void UnsubscribeItem(HotKeyEditItemViewModel item)
        {
            item.UpdateItemRequested -= OnUpdateItemRequested;
            item.MoveUpRequested -= MoveUpItem;
            item.MoveDownRequested -= MoveDownItem;
            item.DeleteRequested -= DeleteItem;
        }

        private void OnModelItemUpdated(object? sender, EventArgs e) => RefreshItems();

        private void OnUpdateItemRequested((HotKeyEditItemViewModel source, HotKeyRegisterItem item) data)
        {
            var index = Items.IndexOf(data.source);
            _model.SetItem(index, data.item);
        }

        private void ResetToDefault() => SettingResetUtils.ResetSingleCategoryAsync(() => _model.ResetToDefault());
    }
}
