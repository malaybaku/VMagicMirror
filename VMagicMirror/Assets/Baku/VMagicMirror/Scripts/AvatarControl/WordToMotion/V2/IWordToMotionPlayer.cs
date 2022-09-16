namespace Baku.VMagicMirror
{
    /// <summary>
    /// Word to Motionの特定のモーションが実行可能なインターフェースを定義します
    /// </summary>
    public interface IWordToMotionPlayer
    {
        //NOTE: IsPlayingが有効なPlayerは1つまでしか存在しない。
        //モーションがフェードインする瞬間からtrueになり、フェードアウトし始める時点でfalseになる
        bool IsPlaying { get; }
        
        /// <summary>
        /// モーション実行中にIKや指の操作があると邪魔な場合、このフラグをtrueにすることで、
        /// Runnerクラス側がIKや指の処理を無効にします。
        /// </summary>
        bool UseIkAndFingerFade { get; }

        //NOTE: TryPlayにするという手もあるかも
        bool CanPlay(MotionRequest request);
        void Play(MotionRequest request, out float duration);

        /// <summary>
        /// 別のモーションが開始するときに「なんか実行中の処理あったら中断してね」というニュアンスで呼ばれる
        /// これが呼ばれないでもPlay()後にモーションは勝手に終わる or IKとFingerのFadeによって元に戻る事が期待されている
        /// </summary>
        void Abort();

        
        //NOTE: Play()と異なり、1回呼んだらループ再生になり、同じモーションを指定され続ける限りそれは無視してよい
        void PlayPreview(MotionRequest request);
        void StopPreview();
    }    
}
