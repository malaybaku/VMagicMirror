namespace Baku.VMagicMirrorConfig.ViewModel
{
    /// <summary> Word to Motion機能のコントロールに利用できるデバイスの選択肢1つに相当するViewModelです。 </summary>
    public class WordToMotionDeviceItemViewModel : ViewModelBase
    {
        private WordToMotionDeviceItemViewModel(int index, string displayNameKeySuffix)
        {
            Index = index;
            _displayNameKeySuffix = displayNameKeySuffix;
            LanguageSelector.Instance.LanguageChanged += RefreshDisplayName;
            RefreshDisplayName();
        }

        public int Index { get; }

        private const string DisplayNameKeyPrefix = "WordToMotion_DeviceItem_";
        private readonly string _displayNameKeySuffix;

        private string _displayName = "";
        public string DisplayName
        {
            get => _displayName;
            private set => SetValue(ref _displayName, value);
        }

        internal void RefreshDisplayName()
            => DisplayName = LocalizedString.GetString(DisplayNameKeyPrefix + _displayNameKeySuffix);

        private static WordToMotionDeviceItemViewModel None() => new(WordToMotionSetting.DeviceTypes.None, "None");
        private static WordToMotionDeviceItemViewModel KeyboardTyping() => new(WordToMotionSetting.DeviceTypes.KeyboardWord, "KeyboardWord");
        private static WordToMotionDeviceItemViewModel Gamepad() => new(WordToMotionSetting.DeviceTypes.Gamepad, "Gamepad");
        private static WordToMotionDeviceItemViewModel KeyboardNumKey() => new(WordToMotionSetting.DeviceTypes.KeyboardTenKey, "KeyboardTenKey");
        private static WordToMotionDeviceItemViewModel MidiController() => new(WordToMotionSetting.DeviceTypes.MidiController, "MidiController");

        public static WordToMotionDeviceItemViewModel[] LoadAvailableItems()
            => new[]
            {
                None(),
                KeyboardTyping(),
                Gamepad(),
                KeyboardNumKey(),
                MidiController(),
            };
    }
}
