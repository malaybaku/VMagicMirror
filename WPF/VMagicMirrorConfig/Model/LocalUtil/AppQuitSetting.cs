namespace Baku.VMagicMirrorConfig
{
    class AppQuitSetting
    {
        /// <summary>
        /// アプリケーション終了時にオートセーブを行わず、かつ再起動をするときに立つフラグ。
        /// 全設定のリセットを行うときだけtrueになる
        /// </summary>
        public bool SkipAutoSaveAndRestart { get; set; }
    }
}
