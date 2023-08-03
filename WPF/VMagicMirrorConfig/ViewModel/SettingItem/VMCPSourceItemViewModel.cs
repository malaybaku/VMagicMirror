namespace Baku.VMagicMirrorConfig.ViewModel
{
    //NOTE: コマンドベースでApplyしないと適用されんという使い方にする (これはポート番号の編集が絡むため)。
    //そのため、RPropertyは基本使わず、
    public class VMCPSourceItemViewModel : ViewModelBase
    {
        //NOTE: Design Mode中のプレビュー用に定義してる
        public VMCPSourceItemViewModel()
        {
            Name = new("");
            Port = new("");
            PortNumberIsInvalid = new(false);
            ReceiveHeadPose = new(false);
            ReceiveFacial = new(false);
            ReceiveHandPose = new(false);
            ResetCommand = new ActionCommand(ResetContent);
        }

        public VMCPSourceItemViewModel(VMCPSource model, VMCPSettingViewModel parent)
        {
            PortNumberIsInvalid = new RProperty<bool>(false);
            _parent = parent;

            Name = new RProperty<string>(model.Name, _ => SetDirty());
            Port = new RProperty<string>(model.Port.ToString(), v =>
            {
                //フォーマット違反になってないかチェック。空欄の場合、ポートの利用予定が無いものと見て正常系扱いする
                PortNumberIsInvalid.Value = 
                    !string.IsNullOrEmpty(v) &&
                    !(int.TryParse(v, out int i) && i >= 0 && i < 65536);
                if (PortNumberIsInvalid.Value)
                {
                    SetDirty();
                }
            });

            ReceiveHeadPose = new RProperty<bool>(model.ReceiveHeadPose, _ => SetDirty());
            ReceiveFacial = new RProperty<bool>(model.ReceiveFacial, _ => SetDirty());
            ReceiveHandPose = new RProperty<bool>(model.ReceiveHandPose, _ => SetDirty());

            ResetCommand = new ActionCommand(ResetContent);
        }

        private readonly VMCPSettingViewModel? _parent;

        public RProperty<string> Name { get; }
        //NOTE: 実際は0-65535の間の数字であってほしい
        public RProperty<string> Port { get; }

        public RProperty<bool> PortNumberIsInvalid { get; }

        public RProperty<bool> ReceiveHeadPose { get; set; }
        public RProperty<bool> ReceiveFacial { get; set; }
        public RProperty<bool> ReceiveHandPose { get; set; }

        public ActionCommand ResetCommand { get; }

        public VMCPSource CreateSetting()
        {
            var rawPort = Port.Value;
            var port =
                !string.IsNullOrEmpty(rawPort) && int.TryParse(rawPort, out int i) && i >= 0 && i < 65536
                ? i
                : 0;

            return new VMCPSource()
            {
                Name = Name.Value,
                Port = port,
                ReceiveHeadPose = ReceiveHeadPose.Value,
                ReceiveFacial = ReceiveFacial.Value,
                ReceiveHandPose = ReceiveHandPose.Value,
            };
        }

        private async void ResetContent()
        {
            var indication = MessageIndication.ResetVmcpSourceItemConfirmation();
            var result = await MessageBoxWrapper.Instance.ShowAsync(
                indication.Title,
                indication.Content,
                MessageBoxWrapper.MessageBoxStyle.OKCancel
                );

            if (result)
            {
                Name.Value = "";
                Port.Value = "0";
                ReceiveHeadPose.Value = false;
                ReceiveFacial.Value = false;
                ReceiveHandPose.Value = false;
            }
        }

        private void SetDirty() => _parent?.SetDirty();
    }
}
