namespace Baku.VMagicMirrorConfig
{
    /// <summary>
    /// マイクの音量チェックができるすごいやつだよ
    /// </summary>
    class MicrophoneStatus
    {
        public MicrophoneStatus() : this(
            ModelResolver.Instance.Resolve<IMessageSender>(),
            ModelResolver.Instance.Resolve<IMessageReceiver>(),
            ModelResolver.Instance.Resolve<MotionSettingModel>()
            )
        {
        }

        public MicrophoneStatus(IMessageSender sender, IMessageReceiver receiver, MotionSettingModel motionSetting)
        {
            _sender = sender;
            _motionSetting = motionSetting;

            ShowMicrophoneVolume = new RProperty<bool>(false, show =>
            {
                SetMicrophoneVolumeVisibility(show);
                if (!show)
                {
                    MicrophoneVolumeValue.Value = 0;
                }
            });

            _motionSetting.EnableLipSync.PropertyChanged += (_, __) =>
            {
                if (!_motionSetting.EnableLipSync.Value)
                {
                    ShowMicrophoneVolume.Value = false;
                }
            };

            receiver.ReceivedCommand += OnReceiveCommand;

        }

        private readonly IMessageSender _sender;
        private readonly MotionSettingModel _motionSetting;

        public RProperty<bool> ShowMicrophoneVolume { get; }

        /// <summary>
        /// NOTE: 0 ~ 20が無音、21~40が適正、41~50がデカすぎになる。これはUnity側がそういう整形をしてくれる
        /// </summary>
        public RProperty<int> MicrophoneVolumeValue { get; } = new RProperty<int>(0);

        private void OnReceiveCommand(CommandReceivedData e)
        {
            if (e.Command is VMagicMirror.VmmServerCommands.MicrophoneVolumeLevel && 
                ShowMicrophoneVolume.Value && 
                int.TryParse(e.GetStringValue(), out int i))
            {
                MicrophoneVolumeValue.Value = i;
            }
        }

        private void SetMicrophoneVolumeVisibility(bool visible)
            => _sender.SendMessage(MessageFactory.SetMicrophoneVolumeVisibility(visible));

    }
}
