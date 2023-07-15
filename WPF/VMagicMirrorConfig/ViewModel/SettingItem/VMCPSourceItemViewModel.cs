namespace Baku.VMagicMirrorConfig.ViewModel
{
    //NOTE: コマンドベースでApplyしないと適用されんという使い方にする (これはポート番号の編集が絡むため)。
    //そのため、RPropertyは基本使わず、
    public class VMCPSourceItemViewModel : ViewModelBase
    {
        //TODO: モデルが持ってる初期値を受け取りたい
        public VMCPSourceItemViewModel()
        {
            PortNumberIsInvalid = new RProperty<bool>(false);
            Port = new RProperty<string>("", v =>
            {
                //フォーマット違反になってないかチェック。空欄の場合、ポートの利用予定が無いものと見て正常系扱いする
                PortNumberIsInvalid.Value = 
                    !string.IsNullOrEmpty(v) &&
                    !(int.TryParse(v, out int i) && i >= 0 && i < 65536);
            });

            ResetCommand = new ActionCommand(ResetContent);
        }

        public string Name { get; set; } = "";

        //NOTE: 実際は0-65535の間の数字であってほしい
        public RProperty<string> Port { get; }

        public RProperty<bool> PortNumberIsInvalid { get; }

        public bool ReceiveHeadPose { get; set; }
        public bool ReceiveFacial { get; set; }
        public bool ReceiveHandPose { get; set; }

        public ActionCommand ResetCommand { get; }

        public void ResetContent()
        {
            Name = "";
            Port.Value = "0";
            ReceiveHeadPose = false;
            ReceiveFacial = false;
            ReceiveHandPose = false;
        }
    }
}
