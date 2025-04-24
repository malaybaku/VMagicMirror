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
                setting.VMCPEnabled, v => SendMessage(MessageFactory.EnableVMCP(v)));
            SerializedVMCPSourceSetting = new(
                setting.SerializedVMCPSourceSetting, v => SendMessage(MessageFactory.SetVMCPSources(v)));

            DisableCameraDuringVMCPActive = new(
                setting.DisableCameraDuringVMCPActive,
                v => SendMessage(MessageFactory.SetDisableCameraDuringVMCPActive(v))
                );
            EnableNaiveBoneTransfer = new(
                setting.EnableNaiveBoneTransfer,
                v => SendMessage(MessageFactory.SetVMCPNaiveBoneTransfer(v))
                );

            VMCPSendEnabled = new(
                setting.VMCPSendEnabled,
                v => SendMessage(MessageFactory.EnableVMCPSend(v))
                );
            SerializedVMCPSendSetting = new(                
                setting.SerializedVMCPSendSetting,
                v => SendMessage(MessageFactory.SetVMCPSendSettings(v))
                );
            ShowEffectDuringVMCPSendEnabled = new(
                setting.ShowEffectDuringVMCPSendEnabled,
                v => SendMessage(MessageFactory.ShowEffectDuringVMCPSendEnabled(v))
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

        // 送信系のプロパティ
        public RProperty<bool> VMCPSendEnabled { get; }

        // NOTE: 設計がちょっと捻れているが、多分悪さはしないのでほっといてある
        // (受信側の Serialized~ 相当の部分と EnableNaive~ 等の設定が全部1本にシリアライズされてる)
        public RProperty<string> SerializedVMCPSendSetting { get; }
        public RProperty<bool> ShowEffectDuringVMCPSendEnabled { get; }

        public event EventHandler? ConnectedStatusChanged;

        private void OnReceiveCommand(CommandReceivedData e)
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

        public void SetSendBonePose(bool value)
        {
            var setting = GetCurrentSendSetting();
            if (setting.SendBonePose != value)
            {
                setting.SendBonePose = value;
                SetVMCPSendSetting(setting);
            }
        }

        public void SetSendFingerBonePose(bool value)
        {
            var setting = GetCurrentSendSetting();
            if (setting.SendFingerBonePose != value)
            {
                setting.SendFingerBonePose = value;
                SetVMCPSendSetting(setting);
            }
        }

        public void SetSendFacial(bool value)
        {
            var setting = GetCurrentSendSetting();
            if (setting.SendFacial != value)
            {
                setting.SendFacial = value;
                SetVMCPSendSetting(setting);
            }
        }

        public void SetSendNonStandardFacial(bool value)
        {
            var setting = GetCurrentSendSetting();
            if (setting.SendNonStandardFacial != value)
            {
                setting.SendNonStandardFacial = value;
                SetVMCPSendSetting(setting);
            }
        }

        public void SetUseVrm0Facial(bool value)
        {
            var setting = GetCurrentSendSetting();
            if (setting.UseVrm0Facial != value)
            {
                setting.UseVrm0Facial = value;
                SetVMCPSendSetting(setting);
            }
        }

        public void SetPrefer30Fps(bool value)
        {
            var setting = GetCurrentSendSetting();
            if (setting.Prefer30Fps != value)
            {
                setting.Prefer30Fps = value;
                SetVMCPSendSetting(setting);
            }
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
