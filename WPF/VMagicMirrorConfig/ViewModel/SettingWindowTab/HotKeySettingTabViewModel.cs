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
                RefreshItems();
            }
        }

        private readonly HotKeySettingModel _model;

        public RProperty<bool> EnableHotKey => _model.EnableHotKey;

        public ObservableCollection<HotKeyEditItemViewModel> Items { get; } 
            = new ObservableCollection<HotKeyEditItemViewModel>();

        private ActionCommand? _resetCommand;
        public ActionCommand ResetCommand => _resetCommand ??= new ActionCommand(ResetToDefault);

        private void RefreshItems()
        {
            foreach (var item in Items)
            {
                item.UpdateItemRequested -= OnUpdateItemRequested;
            }
            Items.Clear();

            foreach (var item in _model.Items)
            {
                var vm = new HotKeyEditItemViewModel(item);
                vm.UpdateItemRequested += OnUpdateItemRequested;
                Items.Add(vm);
            }
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
