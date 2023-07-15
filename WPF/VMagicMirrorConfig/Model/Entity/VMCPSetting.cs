namespace Baku.VMagicMirrorConfig
{
    public class VMCPSetting : SettingEntityBase
    {
        public bool VMCPEnabled { get; set; }
        public string SerializedVMCPSourceSetting { get; set; } = "";

        public void Reset()
        {
            VMCPEnabled = false;
            SerializedVMCPSourceSetting = "";
        }

        //使用者側が書き換えない前提でラフに公開してる
        public static VMCPSetting Default { get; } = new();
    }
}
