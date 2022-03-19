using System;

namespace Baku.VMagicMirrorConfig
{
    class AutomationSettingModel : SettingModelBase<AutomationSetting>, IDisposable
    {
        public AutomationSettingModel(IMessageSender sender) : base(sender)
        {
            var setting = AutomationSetting.Default;

            IsAutomationEnabled = new RProperty<bool>(setting.IsAutomationEnabled, v => 
            {
                if (IsReadyToReceive())
                {
                    StartReceiveInput();
                }
                else
                {
                    StopReceiveInput();
                }
            });

            AutomationPortNumber = new RProperty<int>(setting.AutomationPortNumber, i =>
            {
                if (IsReadyToReceive())
                {
                    //ポート番号が変わった場合、止めて再スタートする
                    if (_receiver.IsRunning)
                    {
                        StopReceiveInput();
                    }
                    StartReceiveInput();
                }
                else
                {
                    StopReceiveInput();
                }
            });
        }

        private readonly AutomationInputReceiver _receiver = new AutomationInputReceiver();

        public event Action<LoadSettingFileArgs>? LoadSettingFileRequested
        {
            add => _receiver.LoadSettingFileRequested += value;
            remove => _receiver.LoadSettingFileRequested -= value;
        }

        //NOTE: お作法的にSyncを挟んでいるが、実際はUnityに直接共有するような設定は現状なくて、
        //設定ファイルのリロードに関する処理だけを持っている。

        public RProperty<bool> IsAutomationEnabled { get; }
        public RProperty<int> AutomationPortNumber { get; }

        private bool IsReadyToReceive()
        {
            return IsAutomationEnabled.Value &&
                AutomationPortNumber.Value >= 0 && 
                AutomationPortNumber.Value < 65536;
        }

        //TODO: この辺で入力を受け付けたらファイルのロードリクエストをしたい

        private void StartReceiveInput() => _receiver.Start(AutomationPortNumber.Value);
        private void StopReceiveInput() => _receiver.Stop();

        public override void ResetToDefault()
        {
            var setting = AutomationSetting.Default;
            IsAutomationEnabled.Value = setting.IsAutomationEnabled;
            AutomationPortNumber.Value = setting.AutomationPortNumber;
        }

        public void Dispose() => StopReceiveInput();
    }
}
