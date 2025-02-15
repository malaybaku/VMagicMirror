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
            var setting = VMCPSetting.Default;
            VMCPEnabled = new(
                setting.VMCPEnabled, v => SendMessage(MessageFactory.Instance.EnableVMCP(v)));
            SerializedVMCPSourceSetting = new(
                setting.SerializedVMCPSourceSetting, v => SendMessage(MessageFactory.Instance.SetVMCPSources(v)));

            DisableCameraDuringVMCPActive = new(
                setting.DisableCameraDuringVMCPActive,
                v => SendMessage(MessageFactory.Instance.SetDisableCameraDuringVMCPActive(v))
                );
            EnableNaiveBoneTransfer = new(
                setting.EnableNaiveBoneTransfer,
                v => SendMessage(MessageFactory.Instance.SetVMCPNaiveBoneTransfer(v))
                );

            VMCPSendEnabled = new(
                setting.VMCPSendEnabled,
                v => SendMessage(MessageFactory.Instance.EnableVMCPSend(v))
                );
            SerializedVMCPSendSetting = new(                
                setting.SerializedVMCPSendSetting,
                v => SendMessage(MessageFactory.Instance.SetVMCPSendSettings(v))
                );
            ShowEffectDuringVMCPSendEnabled = new(
                setting.ShowEffectDuringVMCPSendEnabled,
                v => SendMessage(MessageFactory.Instance.ShowEffectDuringVMCPSendEnabled(v))
                );

            receiver.ReceivedCommand += OnReceiveCommand;
        }

        // 受信系のプロパティ
        public RProperty<bool> VMCPEnabled { get; }
        public RProperty<string> SerializedVMCPSourceSetting { get; }

        public RProperty<bool> EnableNaiveBoneTransfer { get; }
        public RProperty<bool> DisableCameraDuringVMCPActive { get; }

        private readonly VMCPReceiveStatus _receiveStatus = new();
        public IReadOnlyList<bool> Connected => _receiveStatus.Connected;

        // 送信系のプロパティ: 送信のほうがシンプル
        public RProperty<bool> VMCPSendEnabled { get; }
        public RProperty<string> SerializedVMCPSendSetting { get; }
        public RProperty<bool> ShowEffectDuringVMCPSendEnabled { get; }

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

        public VMCPSources GetCurrentReceiveSetting()
        {
            return SerializedVMCPSources
                .FromJson(SerializedVMCPSourceSetting.Value)
                .ToSetting();
        }

        public void SetVMCPSourceSetting(VMCPSources setting)
        {
            SerializedVMCPSourceSetting.Value = SerializedVMCPSources.FromSetting(setting).ToJson();
        }

        public VMCPSendSetting GetCurrentSendSetting()
        {
            return SerializedVMCPSendSettings
                .FromJson(SerializedVMCPSendSetting.Value)
                .ToSetting();
        }

        public void SetVMCPSendSetting(VMCPSendSetting setting)
        {
            SerializedVMCPSendSetting.Value = SerializedVMCPSendSettings.FromSetting(setting).ToJson();
        }

        public override void ResetToDefault()
        {
            var defaultSetting = VMCPSetting.Default;
            VMCPEnabled.Value = defaultSetting.VMCPEnabled;
            SerializedVMCPSourceSetting.Value = defaultSetting.SerializedVMCPSourceSetting;
            DisableCameraDuringVMCPActive.Value = defaultSetting.DisableCameraDuringVMCPActive;

            VMCPEnabled.Value = defaultSetting.VMCPEnabled;
            SerializedVMCPSendSetting.Value = defaultSetting.SerializedVMCPSendSetting;
            ShowEffectDuringVMCPSendEnabled.Value = defaultSetting.ShowEffectDuringVMCPSendEnabled;
        }
    }
}
