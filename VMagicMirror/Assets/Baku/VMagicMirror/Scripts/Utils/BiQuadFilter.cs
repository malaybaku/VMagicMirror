using UnityEngine;

namespace Baku.VMagicMirror
{
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

            var a0Inv  = 1f / (1f + alpha);

            A1 = -2f * cos;
            A2 = 1f - alpha;
            B0 = (1f - cos)  * 0.5f;
            B1 = 1f - cos;
            B2 = B0;

            A1 *= a0Inv;
            A2 *= a0Inv;
            B0 *= a0Inv;
            B1 *= a0Inv;
            B2 *= a0Inv;
        }

        /// <summary>
        /// 他のFilterのパラメータをコピーする。
        /// 同一の補間パラメータを複数のFilterで共有する場合、Filterの一つで SetUp~ 関数を呼んでから本関数を使うほうが
        /// 三角関数の計算がちょっとだけケチれる
        /// </summary>
        /// <param name="src"></param>
        public void CopyParametersFrom(BiQuadFilter src)
        {
            A1 = src.A1;
            A2 = src.A2;
            B0 = src.B0;
            B1 = src.B1;
            B2 = src.B2;
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

        /// <summary>
        /// 3つのフィルタに全て同じパラメータをセットする
        /// </summary>
        /// <param name="samplingRate"></param>
        /// <param name="cutOffFrequency"></param>
        public void SetUpAsLowPassFilter(float samplingRate, float cutOffFrequency)
        {
            _x.SetUpAsLowPassFilter(samplingRate, cutOffFrequency, 1 / Mathf.Sqrt(2));
            _y.CopyParametersFrom(_x);
            _z.CopyParametersFrom(_x);
        }
        
        public void CopyParametersFrom(BiQuadFilterVector3 src)
        {
            _x.CopyParametersFrom(src._x);
            _y.CopyParametersFrom(src._y);
            _z.CopyParametersFrom(src._z);
        }
        
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
    /// 回転を平滑化するフィルタで、特に回転差分をSO(3)に変換して補間することによって平滑化を行うもの。
    /// NOTE:
    /// - Naive版より性能が良いはずなので原則こっちを使う
    /// - SO(3)が概念的にやや分かりにくいのだけ注意
    /// </summary>
    public class BiQuadFilterQuaternion
    {
        private readonly BiQuadFilterVector3 _so3Filter = new();

        public Quaternion Output { get; private set; } = Quaternion.identity;

        public void SetUpAsLowPassFilter(float samplingRate, float cutOffFrequency)
            => _so3Filter.SetUpAsLowPassFilter(samplingRate, cutOffFrequency);

        public void CopyParametersFrom(BiQuadFilterQuaternion src)
            => _so3Filter.CopyParametersFrom(src._so3Filter);

        public void ResetValue(Quaternion value)
        {
            var so3 = So3.Log(value);
            _so3Filter.ResetValue(so3);
            Output = value;
        }

        public Quaternion Update(Quaternion value)
        {
            var so3 = So3.Log(value);
            var filtered = _so3Filter.Update(so3);
            Output = So3.Exp(filtered);
            return Output;
        }
    }

    /// <summary>
    /// 回転を平滑化するやつで、回転軸と回転角度それぞれを平滑化するような実装を持つもの。
    /// NOTE:
    /// - この方法よりSO(3)に変換するほうが筋が良さそう
    /// - この実装は回転角度が180度付近になるケースに弱いのが既知
    /// </summary>
    public class BiQuadFilterQuaternionNaive
    {
        private readonly BiQuadFilterVector3 _axisFilter = new();
        private readonly BiQuadFilter _angleFilter = new();

        public Quaternion Output { get; private set; } = Quaternion.identity;

        public void SetUpAsLowPassFilter(float samplingRate, float cutOffFrequency)
        {
            _axisFilter.SetUpAsLowPassFilter(samplingRate, cutOffFrequency);
            _angleFilter.SetUpAsLowPassFilter(samplingRate, cutOffFrequency);
        }

        public void CopyParametersFrom(BiQuadFilterQuaternionNaive src)
        {
            _axisFilter.CopyParametersFrom(src._axisFilter);
            _angleFilter.CopyParametersFrom(src._angleFilter);
        }

        public void ResetValue(Quaternion value)
        {
            var axisAngle = ToAxisAngle(value);
            _axisFilter.ResetValue(axisAngle.axis);
            _angleFilter.ResetValue(axisAngle.angle);
            Output = value;
        }

        public Quaternion Update(Quaternion input)
        {
            var axisAngle = ToAxisAngle(input);
            var filteredAxis = _axisFilter.Update(axisAngle.axis).normalized;
            var filteredAngle = _angleFilter.Update(axisAngle.angle);
            Output = Quaternion.AngleAxis(filteredAngle, filteredAxis);
            return Output;
        }

        private static (Vector3 axis, float angle) ToAxisAngle(Quaternion q)
        {
            q.ToAngleAxis(out var angleResult, out var axisResult);
            return (axisResult, angleResult);
        }
    }
}
