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

        public void MoveUp(HotKeyEditItemViewModel item)
        {
            var index = Items.IndexOf(item);
            if (index > 0)
            {
                Items.Move(index, index - 1);
                _model.MoveUp(index);
            }
        }

        public void MoveDown(HotKeyEditItemViewModel item)
        {
            var index = Items.IndexOf(item);
            if (index >= 0 && index < Items.Count - 1)
            {
                Items.Move(index, index + 1);
                _model.MoveDown(index);
            }
        }

        public void Delete(HotKeyEditItemViewModel item)
        {
            var index = Items.IndexOf(item);
            if (index >= 0 && index < Items.Count)
            {
                item.UpdateItemRequested -= OnUpdateItemRequested;
                Items.RemoveAt(index);
                _model.Delete(index);
            }
        }

        public void AddNewItem()
        {
            _model.AddNewItem();
            var vm = new HotKeyEditItemViewModel(_model.Items[^1]);
            vm.UpdateItemRequested += OnUpdateItemRequested;
            Items.Add(vm);
        }

        private void OnModelItemUpdated(object? sender, EventArgs e) => RefreshItems();

        private void OnUpdateItemRequested((HotKeyEditItemViewModel source, HotKeyRegisterItem item) data)
        {
            var index = Items.IndexOf(data.source);
            _model.SetItem(index, data.item);
        }

        private void ResetToDefault() => SettingResetUtils.ResetSingleCategoryAsync(() => _model.ResetToDefault());
    }

    //このインスタンスはアプリケーションで1回作ったらずっと使い回す
    public class HotKeySupportedActionViewModel
    {
        private const string None = "Hotkey_Action_None";
        private const string SetCameraFormat = "Hotkey_Action_SetCamera_Format";
        private const string CallWtmFormat = "Hotkey_Action_CallWtm_Format";

        public static HotKeySupportedActionViewModel[] AvailableHotKeyActions { get; }

        static HotKeySupportedActionViewModel()
        {
            AvailableHotKeyActions = new HotKeySupportedActionViewModel[]
            {
                new (new (HotKeyActions.None, 0)),
                new (new (HotKeyActions.SetCamera, 1)),
                new (new (HotKeyActions.SetCamera, 2)),
                new (new (HotKeyActions.SetCamera, 3)),
                new (new (HotKeyActions.CallWtm, 1)),
                new (new (HotKeyActions.CallWtm, 2)),
                new (new (HotKeyActions.CallWtm, 3)),
                new (new (HotKeyActions.CallWtm, 4)),
                new (new (HotKeyActions.CallWtm, 5)),
                new (new (HotKeyActions.CallWtm, 6)),
                new (new (HotKeyActions.CallWtm, 7)),
                new (new (HotKeyActions.CallWtm, 8)),
                new (new (HotKeyActions.CallWtm, 9)),
                new (new (HotKeyActions.CallWtm, 10)),
            };
        }

        public HotKeySupportedActionViewModel(HotKeyActionContent content)
        {
            Content = content;
            LanguageSelector.Instance.LanguageChanged += UpdateDisplayContent;
            UpdateDisplayContent();
        }

        public HotKeyActionContent Content { get; }
        public RProperty<string> DisplayContent = new RProperty<string>("");

        private void UpdateDisplayContent()
        {
            var formatKey = Content.Action switch
            {
                HotKeyActions.SetCamera => SetCameraFormat,
                HotKeyActions.CallWtm => CallWtmFormat,
                //ここは通過しないはず
                _ => None,
            };

            if (Content.Action == HotKeyActions.None)
            {
                DisplayContent.Value = LocalizedString.GetString(formatKey);
            }
            else
            {
                DisplayContent.Value = string.Format(
                    LocalizedString.GetString(formatKey),
                    Content.ArgNumber
                );
            }
        }
    }
}
