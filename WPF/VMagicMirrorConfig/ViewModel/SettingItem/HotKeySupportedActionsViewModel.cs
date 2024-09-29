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
            ConstAvailableHotKeyActions = new HotKeySupportedActionViewModel[14 + 40];
            ConstAvailableHotKeyActions[0] = new(new(HotKeyActions.None, 0, ""));
            ConstAvailableHotKeyActions[1] = new(new(HotKeyActions.SetCamera, 1, ""));
            ConstAvailableHotKeyActions[2] = new(new(HotKeyActions.SetCamera, 2, ""));
            ConstAvailableHotKeyActions[3] = new(new(HotKeyActions.SetCamera, 3, ""));
            ConstAvailableHotKeyActions[4] = new(new(HotKeyActions.SetBodyMotionStyle, (int)HotKeyActionBodyMotionStyle.Default, ""));
            ConstAvailableHotKeyActions[5] = new(new(HotKeyActions.SetBodyMotionStyle, (int)HotKeyActionBodyMotionStyle.AlwaysHandDown, ""));
            ConstAvailableHotKeyActions[6] = new(new(HotKeyActions.SetBodyMotionStyle, (int)HotKeyActionBodyMotionStyle.GameInputLocomotion, ""));

            ConstAvailableHotKeyActions[7] = new(new(HotKeyActions.ToggleVMCPReceiveActive, 0, ""));
            ConstAvailableHotKeyActions[8] = new(new(HotKeyActions.ToggleKeyboardVisibility, 0, ""));
            ConstAvailableHotKeyActions[9] = new(new(HotKeyActions.TogglePenVisibility, 0, ""));
            ConstAvailableHotKeyActions[10] = new(new(HotKeyActions.ToggleGamepadVisibility, 0, ""));
            ConstAvailableHotKeyActions[11] = new(new(HotKeyActions.ToggleShadowVisibility, 0, ""));
            ConstAvailableHotKeyActions[12] = new(new(HotKeyActions.ToggleOutlineVisibility, 0, ""));
            ConstAvailableHotKeyActions[13] = new(new(HotKeyActions.ToggleWindVisibility, 0, ""));


            for (var i = 0; i < 40; i++)
            {
                ConstAvailableHotKeyActions[14 + i] = new(new(HotKeyActions.CallWtm, i + 1, ""));
            }
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
                        new HotKeyActionContent(HotKeyActions.ToggleAccessory, 0, accessory.FileId), 
                        _accessorySetting
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
        private const string DisplayNameKeyPrefix = "Hotkey_Action_";
        private const string None = DisplayNameKeyPrefix + "None";
        private const string SetCameraFormat = DisplayNameKeyPrefix + "SetCamera_Format";
        private const string CallWtmFormat = DisplayNameKeyPrefix + "CallWtm_Format";
        private const string ToggleAccessoryFormat = DisplayNameKeyPrefix + "ToggleAccessory_Format";
        private const string SetBodyMotionStylePrefix = DisplayNameKeyPrefix + "SetBodyMotionStyle_";

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
                case HotKeyActions.SetBodyMotionStyle:
                    var suffix = Content.ArgNumber switch
                    {
                        (int)HotKeyActionBodyMotionStyle.Default => "Default",
                        (int)HotKeyActionBodyMotionStyle.AlwaysHandDown => "AlwaysHandDown",
                        (int)HotKeyActionBodyMotionStyle.GameInputLocomotion => "GameInputLocomotion",
                        _ => "Unknown",
                    };
                    DisplayName = LocalizedString.GetString(SetBodyMotionStylePrefix + suffix);
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

                case HotKeyActions.ToggleVMCPReceiveActive:
                    DisplayName = LocalizedString.GetString(DisplayNameKeyPrefix + nameof(HotKeyActions.ToggleVMCPReceiveActive));
                    break;
                case HotKeyActions.ToggleKeyboardVisibility:
                    DisplayName = LocalizedString.GetString(DisplayNameKeyPrefix + nameof(HotKeyActions.ToggleKeyboardVisibility));
                    break;
                case HotKeyActions.TogglePenVisibility:
                    DisplayName = LocalizedString.GetString(DisplayNameKeyPrefix + nameof(HotKeyActions.TogglePenVisibility));
                    break;
                case HotKeyActions.ToggleShadowVisibility:
                    DisplayName = LocalizedString.GetString(DisplayNameKeyPrefix + nameof(HotKeyActions.ToggleShadowVisibility));
                    break;
                case HotKeyActions.ToggleOutlineVisibility:
                    DisplayName = LocalizedString.GetString(DisplayNameKeyPrefix + nameof(HotKeyActions.ToggleOutlineVisibility));
                    break;
                case HotKeyActions.ToggleWindVisibility:
                    DisplayName = LocalizedString.GetString(DisplayNameKeyPrefix + nameof(HotKeyActions.ToggleWindVisibility));
                    break;
                case HotKeyActions.None:
                default:
                    DisplayName = LocalizedString.GetString(None);
                    break;
            }
        }
    }
}
