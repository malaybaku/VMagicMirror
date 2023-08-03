namespace Baku.VMagicMirrorConfig
{
    internal class VMCPSettingModel : SettingModelBase<VMCPSetting>
    {
        public VMCPSettingModel() : this(ModelResolver.Instance.Resolve<IMessageSender>())
        {
        }

        public VMCPSettingModel(IMessageSender sender) : base(sender)
        {
            var defaultSetting = VMCPSetting.Default;
            VMCPEnabled = new(
                defaultSetting.VMCPEnabled, v => SendMessage(MessageFactory.Instance.EnableVMCP(v)));
            SerializedVMCPSourceSetting = new(
                defaultSetting.SerializedVMCPSourceSetting, v => SendMessage(MessageFactory.Instance.SetVMCPSources(v)));

            DisableCameraDuringVMCPActive = new(
                defaultSetting.DisableCameraDuringVMCPActive,
                v => SendMessage(MessageFactory.Instance.SetDisableCameraDuringVMCPActive(v))
                );
            DisableMicDuringVMCPFacialActive = new(
                defaultSetting.DisableMicDuringVMCPFacialActive,
                v => SendMessage(MessageFactory.Instance.SetDisableMicDuringVMCPFacialActive(v))
                );
        }

        public RProperty<bool> VMCPEnabled { get; }
        public RProperty<string> SerializedVMCPSourceSetting { get; }
        public RProperty<bool> DisableCameraDuringVMCPActive { get; }
        public RProperty<bool> DisableMicDuringVMCPFacialActive { get; }

        public VMCPSources GetCurrentSetting()
        {
            return SerializedVMCPSources
                .FromJson(SerializedVMCPSourceSetting.Value)
                .ToSetting();
        }

        public void SetVMCPSourceSetting(VMCPSources setting)
        {
            SerializedVMCPSourceSetting.Value = SerializedVMCPSources.FromSetting(setting).ToSerializedData();
        }

        public override void ResetToDefault()
        {
            var defaultSetting = VMCPSetting.Default;
            VMCPEnabled.Value = defaultSetting.VMCPEnabled;
            SerializedVMCPSourceSetting.Value = defaultSetting.SerializedVMCPSourceSetting;
            DisableCameraDuringVMCPActive.Value = defaultSetting.DisableCameraDuringVMCPActive;
            DisableMicDuringVMCPFacialActive.Value = defaultSetting.DisableMicDuringVMCPFacialActive;
        }
    }
}
