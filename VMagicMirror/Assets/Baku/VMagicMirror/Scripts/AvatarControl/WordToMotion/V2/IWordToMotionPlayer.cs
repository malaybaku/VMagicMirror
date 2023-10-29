namespace Baku.VMagicMirror
{
    /// <summary>
    /// Word to Motionの特定のモーションが実行可能なインターフェースを定義します
    /// </summary>
    public interface IWordToMotionPlayer
    {
        /// <summary>
        /// モーション実行中にIKや指の操作があると邪魔な場合、このフラグをtrueにすることで、
        /// Runnerクラス側がIKや指の処理を無効にします。
        /// </summary>
        bool UseIkAndFingerFade { get; }

        //NOTE: TryPlayにするという手もあるかも
        bool CanPlay(MotionRequest request);
        
        /// <summary>
        /// モーションを実行する。この関数が呼ばれた場合、実装側では<see cref="Stop()"/>を呼ばないでも勝手に通常姿勢に戻るようにする。
        /// </summary>
        /// <param name="request"></param>
        /// <param name="duration"></param>
        void Play(MotionRequest request, out float duration);

        /// <summary>
        /// このPlayerではないPlayerを実行するときや、プレビューの開始、終了時に呼ばれる。
        /// 実装側では姿勢ジャンプが発生しにくいようにモーションの適用をやめるのが期待される。
        /// </summary>
        void Stop();

        //NOTE: Play()と異なり、1回呼んだらループ再生になり、同じモーションを指定され続ける限りそれは無視してよい
        void PlayPreview(MotionRequest request);
        
        /// <summary>
        /// プレビューを停止する。実装側は<see cref="Stop"/>に比べると強引かつ即座にモーションを停止してもよい。
        /// </summary>
        void StopPreview();
    }    
}
