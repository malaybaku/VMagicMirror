namespace Baku.VMagicMirrorConfig
{
    class PreferenceSettingModel
    {
        //NOTE: Automationなどもコッチに増やしてよい
        //public RProperty<string> PreferredLanguageName { get; } = new RProperty<string>("");
        public RProperty<bool> MinimizeOnLaunch { get; } = new(false);

        public RProperty<bool> ShowVMCPTabOnControlPanel { get; } = new(false);
    }
}
