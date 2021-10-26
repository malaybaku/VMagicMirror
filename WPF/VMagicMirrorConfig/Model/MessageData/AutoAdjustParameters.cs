namespace Baku.VMagicMirrorConfig
{
    //NOTE: このクラスはUnityとWPFで同じメンバ構成にすること
    public class AutoAdjustParameters
    {
        public int LengthFromWristToTip { get; set; } = 12;
        public int LengthFromWristToPalm { get; set; } = 6;

        //カメラ位置は不要: ポーリングで取得してるから勝手に反映される
    }
}
