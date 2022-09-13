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
        //NOTE: Play()の場合と異なり、1回呼んだらループ再生になる && 同じ値を指定し続けたら無視してよい
        void PlayPreview(MotionRequest request);
        
        void Stop();
    }    
}
