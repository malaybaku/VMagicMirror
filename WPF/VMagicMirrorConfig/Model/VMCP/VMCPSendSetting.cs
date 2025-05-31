namespace Baku.VMagicMirrorConfig
{
    public class VMCPSendSetting
    {

        public string SendAddress { get; set; } = "127.0.0.1";
        public int SendPort { get; set; } = 9000;

        /// <summary> trueの場合、少なくとも指以外のボーンの姿勢を送信する </summary>
        public bool SendBonePose { get; set; } = true;
        /// <summary> trueの場合、指ボーンの姿勢も送信する。このオプションはsendBone == falseでは無視される </summary>
        public bool SendFingerBonePose { get; set; } = true;
        /// <summary> trueの場合、少なくともVRM1.0の標準ブレンドシェイプを送信する </summary>
        public bool SendFacial { get; set; } = true;
        /// <summary> trueの場合、標準ブレンドシェイプでないものも送信する。パーフェクトシンク対応モデルとかだとデータが膨れることに注意 </summary>
        public bool SendNonStandardFacial { get; set; } = false;
        /// <summary> trueの場合、ブレンドシェイプ名をVRM0相当に変換する。 </summary>
        public bool UseVrm0Facial { get; set; } = true;
        /// <summary> trueの場合、アプリケーションが60fpsで実行していても30FPSでデータを送信しようとする </summary>
        public bool Prefer30Fps { get; set; } = false;


        public static VMCPSendSetting Default() => new();
    }
}
