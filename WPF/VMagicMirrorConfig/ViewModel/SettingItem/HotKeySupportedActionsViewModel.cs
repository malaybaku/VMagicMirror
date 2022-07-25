using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    //このインスタンスはアプリケーションで1回作ったらずっと使い回してもよい(使い回さなくてもよい)
    public class HotKeySupportedActionsViewModel : ViewModelBase
    {
        private readonly static HotKeySupportedActionViewModel[] ConstAvailableHotKeyActions;

        //NOTE: シングルトンにしてるのはインスタンス数を抑えるため、および
        //モデルのイベント購読の方式的にインスタンス生成がかさむとメモリリークしてしまうので、それを避けるため
        private static HotKeySupportedActionsViewModel? _instance;
        public static HotKeySupportedActionsViewModel Instance => 
            _instance ??= new HotKeySupportedActionsViewModel();

        public static ReadOnlyObservableCollection<HotKeySupportedActionViewModel> InstanceItems
            => Instance.Items;

        static HotKeySupportedActionsViewModel()
        {
            ConstAvailableHotKeyActions = new HotKeySupportedActionViewModel[]
            {
                new (new (HotKeyActions.None, 0, "")),
                new (new (HotKeyActions.SetCamera, 1, "")),
                new (new (HotKeyActions.SetCamera, 2, "")),
                new (new (HotKeyActions.SetCamera, 3, "")),
                new (new (HotKeyActions.CallWtm, 1, "")),
                new (new (HotKeyActions.CallWtm, 2, "")),
                new (new (HotKeyActions.CallWtm, 3, "")),
                new (new (HotKeyActions.CallWtm, 4, "")),
                new (new (HotKeyActions.CallWtm, 5, "")),
                new (new (HotKeyActions.CallWtm, 6, "")),
                new (new (HotKeyActions.CallWtm, 7, "")),
                new (new (HotKeyActions.CallWtm, 8, "")),
                new (new (HotKeyActions.CallWtm, 9, "")),
                new (new (HotKeyActions.CallWtm, 10, "")),
            };
        }

        private HotKeySupportedActionsViewModel() : this(
            ModelResolver.Instance.Resolve<AccessorySettingModel>()
            )
        {
        }

        private HotKeySupportedActionsViewModel(AccessorySettingModel accessorySetting)
        {
            _accessorySetting = accessorySetting;

            Items = new (_items);            
            foreach(var item in ConstAvailableHotKeyActions)
            {
                _items.Add(item);
            }

            if (!IsInDesignMode)
            {
                accessorySetting.ItemUpdated += UpdateDisplayName;
                accessorySetting.ItemNameMaybeChanged += UpdateDisplayName;
                accessorySetting.ItemRefreshed += RefreshItems;
                accessorySetting.ItemReloaded += RefreshItems;
                RefreshItems();
            }
        }

        private AccessorySettingModel _accessorySetting;

        private readonly ObservableCollection<HotKeySupportedActionViewModel> _items = new();
        public ReadOnlyObservableCollection<HotKeySupportedActionViewModel> Items { get; }

        private void UpdateDisplayName(AccessoryItemSetting accessoryItem)
        {
            foreach(var item in _items)
            {
                item.UpdateAccessoryDisplayName(accessoryItem);
            }
        }

        private void RefreshItems()
        {
            //Constなアクションの次に、設定ファイルにあるのと同じ順のアクセサリの項目を並べる
            for (var i = 0; i < _accessorySetting.Items.Items.Length; i++)
            {
                var accessory = _accessorySetting.Items.Items[i];

                var vm = _items
                    .Skip(ConstAvailableHotKeyActions.Length)
                    .FirstOrDefault(item => item.Content.ArgString == accessory.FileId);

                if (vm == null)
                {
                    vm = new HotKeySupportedActionViewModel(
                        new HotKeyActionContent(HotKeyActions.ToggleAccessory, 0, accessory.FileId)
                        );
                    _items.Insert(i + ConstAvailableHotKeyActions.Length, vm);
                }
                else
                {
                    //既存アイテムでも名前の扱いが変更してる可能性があるのでチェック
                    vm.UpdateAccessoryDisplayName(accessory);
                }
            }

            //並べ終わったあとであぶれた項目は消すべき項目のはずなので削除
            var expectedCount = ConstAvailableHotKeyActions.Length + _accessorySetting.Items.Items.Length;
            while (_items.Count > expectedCount)
            {
                _items.RemoveAt(_items.Count - 1);
            }
        }
    }

    public class HotKeySupportedActionViewModel : ViewModelBase
    {
        private const string None = "Hotkey_Action_None";
        private const string SetCameraFormat = "Hotkey_Action_SetCamera_Format";
        private const string CallWtmFormat = "Hotkey_Action_CallWtm_Format";
        private const string ToggleAccessoryFormat = "HotKey_Action_ToggleAccessory_Format";

        //NOTE: カメラの指定 / Wtmの呼び出し用のインスタンスでは第二引数をnullにしてもよい
        internal HotKeySupportedActionViewModel(HotKeyActionContent content, AccessorySettingModel? accessorySetting = null)
        {
            Content = content;
            _accessorySetting = accessorySetting;
            LanguageSelector.Instance.LanguageChanged += UpdateDisplayContent;
            UpdateDisplayContent();
        }

        private readonly AccessorySettingModel? _accessorySetting;

        public HotKeyActionContent Content { get; }

        private string _displayName = "";
        public string DisplayName
        {
            get => _displayName;
            private set => SetValue(ref _displayName, value);
        }

        public void UpdateAccessoryDisplayName(AccessoryItemSetting item)
        {
            if (Content.Action == HotKeyActions.ToggleAccessory && 
                Content.ArgString == item.FileId)
            {
                UpdateDisplayContent();
            }
        }

        private void UpdateDisplayContent()
        {
            switch (Content.Action)
            {
                case HotKeyActions.SetCamera:
                    DisplayName = string.Format(
                        LocalizedString.GetString(SetCameraFormat),
                        Content.ArgNumber
                    );
                    break;
                case HotKeyActions.CallWtm:
                    DisplayName = string.Format(
                        LocalizedString.GetString(CallWtmFormat),
                        Content.ArgNumber
                    );
                    break;
                case HotKeyActions.ToggleAccessory:
                    var accessoryDisplayName = "(unknown)";
 
                    if (_accessorySetting == null)
                    {
                        LogOutput.Instance.Write(new InvalidOperationException("Tried to refer accessory settings, but failed"));
                    }
                    else
                    {
                        var accessoryItem = _accessorySetting.Items.Items.FirstOrDefault(i => i.FileId == Content.ArgString);
                        if (accessoryItem != null)
                        {
                            accessoryDisplayName = accessoryItem.Name;
                        }
                        else
                        {
                            accessoryDisplayName = "(missing: " + Content.ArgString + ")";
                        }
                    }

                    DisplayName = string.Format(
                        LocalizedString.GetString(ToggleAccessoryFormat),
                        accessoryDisplayName
                    );
                    break;
                case HotKeyActions.None:
                default:
                    DisplayName = LocalizedString.GetString(None);
                    break;
            }
        }
    }
}
