namespace Baku.VMagicMirrorConfig
{
    public class VMCPSetting : SettingEntityBase
    {
        public bool VMCPEnabled { get; set; }
        public string SerializedVMCPSourceSetting { get; set; } = "";
        public bool DisableCameraDuringVMCPActive { get; set; } = true;
        public bool EnableNaiveBoneTransfer { get; set; } = false;

        public void Reset()
        {
            VMCPEnabled = false;
            SerializedVMCPSourceSetting = "";
            EnableNaiveBoneTransfer = false;
            DisableCameraDuringVMCPActive = true;
        }

        //使用者側が書き換えない前提でラフに公開してる
        public static VMCPSetting Default { get; } = new();
    }
}
