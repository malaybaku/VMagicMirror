namespace Baku.VMagicMirror.Buddy.Api.Interface
{
    // NOTE: エフェクト設定はAPI上では数値の集まりに見えるだけの値なため、インターフェースではなく具象クラスとして定義されている
    
    public class BounceDeformSpriteEffect
    {
        internal float ElapsedTime { get; set; }

        private bool _isActive;
        /// <summary>
        /// trueにするとエフェクトが作動する。
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                if (!value)
                {
                    ElapsedTime = 0;
                }
            }
        }

        private float _intensity = 0.5f;
        /// <summary>
        /// エフェクトの強度を [0, 1] の範囲で指定する。1より大きい値も入れられるが、入れると動きがかなり極端になる。
        /// 初期値は0.5
        /// </summary>
        public float Intensity
        {
            get => _intensity;
            set => _intensity = ParamUtils.Max(value, 0f);
        }

        private float _duration = 0.5f;
        /// <summary>
        /// エフェクトの開始～終了までの周期。すでにエフェクトが起動しているときに書き換えると動きが飛ぶこともある。
        /// 初期値は0.5
        /// 範囲は [0.01, 5]
        /// </summary>
        public float Duration
        {
            get => _duration;
            set => _duration = ParamUtils.Clamp(value, 0.01f, 5f);
        }

        /// <summary>
        /// trueの場合、エフェクトが効き続ける。falseの場合、1回ぶん動くと自動で<see cref="IsActive"/>がfalseになる。
        /// 初期値はtrue
        /// </summary>
        public bool Loop { get; set; } = true;
    }

    public class FloatingSpriteEffect
    {
        internal float ElapsedTime { get; set; }

        private bool _isActive;
        /// <summary>
        /// trueにするとエフェクトが作動する。
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                if (!value)
                {
                    ElapsedTime = 0;
                }
            }
        }

        private float _intensity = 0.5f;
        /// <summary>
        /// 上昇幅を画面座標ベースで [0, 1] で指定する。相場は0.05~0.15くらいのつもり
        /// 初期値は0.05
        /// </summary>
        public float Intensity
        {
            get => _intensity;
            set => _intensity = ParamUtils.Clamp01(value);
        }

        private float _duration = 2f;
        /// <summary>
        /// エフェクトの開始～終了までの周期。すでにエフェクトが起動しているときに書き換えると動きが飛ぶこともある。
        /// 初期値は2
        /// 範囲は [0.1, 10]
        /// </summary>
        public float Duration
        {
            get => _duration;
            set => _duration = ParamUtils.Clamp(value, 0.1f, 10f);
        }

        // Floatはループしかせんやろ…ということでLoopオプションは省いている
    }
    
    public class SpriteEffectApi
    {
        public FloatingSpriteEffect Floating { get; } = new();
        public BounceDeformSpriteEffect BounceDeform { get; } = new();
    }

    internal static class ParamUtils
    {
        public static float Clamp01(float v) => Clamp(v, 0f, 1f);
        public static float Clamp(float v, float min, float max)
        {
            return v < min ? min :
                v > max ? max :
                v;
        }
        public static float Max(float a, float b) => a > b ? a : b;
        public static float Min(float a, float b) => a < b ? a : b;
    }
}
