using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 2次フィルタをベクトルに効かせたい人用のフィルタ。
    /// 本質的には1つのフィルタが別々に効いてるだけで、別に連成とかがあるわけではないです
    /// </summary>
    public class BiQuadFilterVector3
    {
        private readonly BiQuadFilter _x = new();
        private readonly BiQuadFilter _y = new();
        private readonly BiQuadFilter _z = new();

        public Vector3 Output => new(_x.Output, _y.Output, _z.Output);
        
        public void SetUpAsLowPassFilter(float samplingRate, Vector3 cutOffFrequency, Vector3 q)
        {
            _x.SetUpAsLowPassFilter(samplingRate, cutOffFrequency.x, q.x);
            _y.SetUpAsLowPassFilter(samplingRate, cutOffFrequency.y, q.y);
            _z.SetUpAsLowPassFilter(samplingRate, cutOffFrequency.z, q.z);
        }

        public void SetUpAsLowPassFilter(float samplingRate, Vector3 cutOffFrequency)
            => SetUpAsLowPassFilter(samplingRate, cutOffFrequency, Vector3.one / Mathf.Sqrt(2));

        public void ResetValue(Vector3 value)
        {
            _x.ResetValue(value.x);
            _y.ResetValue(value.y);
            _z.ResetValue(value.z);
        }

        public Vector3 Update(Vector3 input)
        {
            _x.Update(input.x);
            _y.Update(input.y);
            _z.Update(input.z);
            return Output;
        }
    }

    /// <summary>
    /// デジタルフィルタの作法に則った2次フィルタ。
    /// </summary>
    public class BiQuadFilter 
    {
        //1つ前、2つ前の入出力
        private float _prev2Input = 0f;
        private float _prev1Input = 0f;
        private float _prev2Output = 0f;
        private float _prev1Output = 0f;

        // - NOTE: A0という定数も本来あるが、これはLPFでは式全体を割るのに使うため、省略します。
        public float A1 { get; set; }
        public float A2 { get; set; }
        public float B0 { get; set; }
        public float B1 { get; set; }
        public float B2 { get; set; }
        
        public float Output { get; private set; }

        /// ※SamplingRateという考え方はUnityのUpdate使うと地味に難敵なので注意

        /// <summary>
        /// フィルタ用のパラメタ群をローパスフィルタ用の値にします。
        /// Q値は1/root(2)になります。
        /// </summary>
        /// <param name="samplingRate"></param>
        /// <param name="cutOffFrequency"></param>
        public void SetUpAsLowPassFilter(float samplingRate, float cutOffFrequency)
            => SetUpAsLowPassFilter(samplingRate, cutOffFrequency, Mathf.Sqrt(2f) * 0.5f);

        /// <summary>
        /// フィルタ用のパラメタ群をローパスフィルタ用の値にします。
        /// </summary>
        /// <param name="samplingRate"></param>
        /// <param name="cutOffFrequency"></param>
        /// <param name="q"></param>
        public void SetUpAsLowPassFilter(float samplingRate, float cutOffFrequency, float q)
        {
            var omega = 2f * Mathf.PI * cutOffFrequency / samplingRate;
            var alpha = Mathf.Sin(omega) / (2f * q);

            var cos = Mathf.Cos(omega);

            var a0inv  = 1f / (1f + alpha);

            A1 = -2f * cos;
            A2 = 1f - alpha;
            B0 = (1f - cos)  * 0.5f;
            B1 = 1f - cos;
            B2 = B0;

            A1 *= a0inv;
            A2 *= a0inv;
            B0 *= a0inv;
            B1 *= a0inv;
            B2 *= a0inv;
        }

        /// <summary>
        /// 現在のフィルタ設定を用いて値を更新します。
        /// 結果として返却する値は<see cref="Output"/>プロパティにも設定されます。
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public float Update(float input)
        {
            Output = B0 * input + B1 * _prev1Input + B2 * _prev2Input
                     - A1 * _prev1Output - A2 * _prev2Output;

            _prev2Input = _prev1Input;
            _prev1Input = input;
            _prev2Output = _prev1Output;
            _prev1Output = Output;

            return Output;
        }

        /// <summary>
        /// 入力値の履歴をすべて同じ値にします。フィルタを一時的に止めるときに呼んでおくと、再開時に安全になります。
        /// </summary>
        /// <param name="value"></param>
        public void ResetValue(float value)
        {
            Output = value;
            _prev1Input = value;
            _prev2Input = value;
            _prev1Output = value;
            _prev2Output = value;
        }
    }
}
