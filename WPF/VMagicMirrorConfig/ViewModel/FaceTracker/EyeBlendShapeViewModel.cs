namespace Baku.VMagicMirrorConfig.ViewModel
{
    public class EyeBlendShapeRangeViewModel
    {

        // NOTE: 暗にパーセンテージを表す整数値が入ってくるのを期待している
        public void SetValue(float value)
        {
            // ここだけ分岐することで、初期値が0のママになるのを防いでいる
            if (!_hasValue)
            {
                Min.Value = value;
                Max.Value = value;
                Now.Value = value;
                Range.Value = 0;
                _hasValue = true;
                return;
            }

            Now.Value = value;
            if (value < Min.Value)
            {
                Min.Value = value;
            }

            if (value > Max.Value)
            {
                Max.Value = value;
            }

            Range.Value = Max.Value - Min.Value;
        }

        public void ResetValues()
        {
            Min.Value = 0;
            Max.Value = 0;
            Now.Value = 0;
            Range.Value = 0;
            _hasValue = false;
        }

        // NOTE: SetValueの1回目とそれ以外が区別できる必要がある
        private bool _hasValue;

        public RProperty<float> Min { get; } = new(0f);
        public RProperty<float> Max { get; } = new(0f);
        public RProperty<float> Now { get; } = new(0f);
        // Max - Min
        public RProperty<float> Range { get; } = new(0f);

    }
}
