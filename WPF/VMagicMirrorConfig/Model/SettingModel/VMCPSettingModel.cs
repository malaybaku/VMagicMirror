using System;
using System.Collections.Generic;

namespace Baku.VMagicMirrorConfig
{
    internal class VMCPSettingModel : SettingModelBase<VMCPSetting>
    {
        public VMCPSettingModel() : this(
            ModelResolver.Instance.Resolve<IMessageSender>(),
            ModelResolver.Instance.Resolve<IMessageReceiver>()
            )
        {
        }

        public VMCPSettingModel(IMessageSender sender, IMessageReceiver receiver) : base(sender)
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

            receiver.ReceivedCommand += OnReceiveCommand;
        }

        public RProperty<bool> VMCPEnabled { get; }
        public RProperty<string> SerializedVMCPSourceSetting { get; }
        public RProperty<bool> DisableCameraDuringVMCPActive { get; }

        private readonly VMCPReceiveStatus _receiveStatus = new();
        public IReadOnlyList<bool> Conneceted => _receiveStatus.Connected;

        public event EventHandler? ConnectedStatusChanged;

        private void OnReceiveCommand(object? sender, CommandReceivedEventArgs e)
        {
            if (e.Command != ReceiveMessageNames.NotifyVmcpReceiveStatus)
            {
                return;
            }

            var changed = _receiveStatus.ApplySerializedStatus(e.Args);
            if (changed)
            {
                ConnectedStatusChanged?.Invoke(this, EventArgs.Empty);
            }
        }

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
        }
    }
}
