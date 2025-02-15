namespace Baku.VMagicMirrorConfig
{
    public class VMCPSetting : SettingEntityBase
    {
        // Receive
        public bool VMCPEnabled { get; set; }
        public string SerializedVMCPSourceSetting { get; set; } = "";
        public bool DisableCameraDuringVMCPActive { get; set; } = true;
        public bool EnableNaiveBoneTransfer { get; set; } = false;

        // Send
        public bool VMCPSendEnabled { get; set; }
        public string SerializedVMCPSendSetting { get; set; } = "";
        public bool ShowEffectDuringVMCPSendEnabled { get; set; } = false;
        
        public void Reset()
        {
            VMCPEnabled = false;
            SerializedVMCPSourceSetting = "";
            EnableNaiveBoneTransfer = false;
            DisableCameraDuringVMCPActive = true;

            VMCPSendEnabled = false;
            SerializedVMCPSendSetting = "";
            ShowEffectDuringVMCPSendEnabled = false;
        }

        //使用者側が書き換えない前提でラフに公開してる
        public static VMCPSetting Default { get; } = new();
    }
}
