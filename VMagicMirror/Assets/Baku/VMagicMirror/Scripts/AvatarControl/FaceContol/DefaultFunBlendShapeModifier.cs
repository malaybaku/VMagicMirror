using UniVRM10;

namespace Baku.VMagicMirror
{
    //TODO: めっちゃ廃止したい～
    /// <summary> 何もしてないときも若干Funのブレンドシェイプを入れるやつ </summary>
    public class DefaultFunBlendShapeModifier
    {
        public float FaceDefaultFunValue { get; set; } = 0.0f;

        public void Apply(ExpressionAccumulator accumulator)
        {
            accumulator.Accumulate(ExpressionKey.Relaxed, FaceDefaultFunValue);
        }
        
        public void Reset(ExpressionAccumulator accumulator)
        {
            accumulator.Accumulate(ExpressionKey.Relaxed, 0f);
        }
    }
}
