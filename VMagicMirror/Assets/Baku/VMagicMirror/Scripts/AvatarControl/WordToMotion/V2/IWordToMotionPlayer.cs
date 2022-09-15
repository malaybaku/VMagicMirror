namespace Baku.VMagicMirror
{
    /// <summary>
    /// Word to Motionの特定のモーションが実行可能なインターフェースを定義します
    /// </summary>
    public interface IWordToMotionPlayer
    {
        //NOTE: TryPlayにするという手もあるかも
        bool CanPlay(MotionRequest request);
        void Play(MotionRequest request, out float duration);
        //NOTE: Play()と異なり、1回呼んだらループ再生になり、同じモーションを指定され続ける限りそれは無視してよい
        void PlayPreview(MotionRequest request);
        
        void Stop();
    }    
}
