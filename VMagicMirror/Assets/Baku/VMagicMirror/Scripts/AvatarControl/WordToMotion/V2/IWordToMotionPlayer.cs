namespace Baku.VMagicMirror
{
    /// <summary>
    /// Word to Motionの特定のモーションが実行可能なインターフェースを定義します
    /// </summary>
    public interface IWordToMotionPlayer
    {
        //NOTE: TryPlayにするという手もあるかも
        bool CanPlay(MotionRequest request);
        void Play(MotionRequest request);
        //NOTE: Play()の場合と異なり、1回呼んだらループ再生になることが期待される
        void PlayPreview(MotionRequest request);
        
        void Stop();
    }    
}
