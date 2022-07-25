using System;

namespace Baku.VMagicMirrorConfig
{
    public class HotKeySettingItemCollection
    {
        public HotKeySettingItem[] Items { get; set; } = Array.Empty<HotKeySettingItem>();
    }

    /// <summary>
    /// シリアライズして保存可能な、ホットキーで発動するアクション１つ分の設定
    /// </summary>
    public class HotKeySettingItem
    {
        public int Action { get; set; }
        public int ActionArgNumber { get; set; }
        public string ActionArgString { get; set; } = "";
        public int Key { get; set; }
        public int ModifierKeys { get; set; }

        public static HotKeySettingItem LoadEmpty() => new HotKeySettingItem()
        {
            Action = 0,
            ActionArgNumber = 0,
            ActionArgString = "",
            Key = 0,
            ModifierKeys = 0,
        };
    }

    public class HotKeySetting : SettingEntityBase
    {
        //NOTE: 規約としてDefaultの書き換えは禁止
        public static readonly HotKeySetting Default = new();

        public bool EnableHotKey { get; set; } = false;
        //NOTE: シリアライズできるなら HotKeySettingItems[] とかで保持できてるほうがbetter
        public string SerializedItems { get; set; } = "";

        public void Reset()
        {
            EnableHotKey = false;
            SerializedItems = Default.SerializedItems;
        }
    }
}
