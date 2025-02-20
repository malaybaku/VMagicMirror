namespace Baku.VMagicMirror.Buddy.Api.Interface
{
    // NOTE: エフェクト設定はAPI上では数値の集まりに見えるだけの値なため、インターフェースではなく具象クラスとして定義されている
    public interface ISpriteEffectApi
    {
        public IFloatingSpriteEffect Floating { get; }
        public IBounceDeformSpriteEffect BounceDeform { get; }
    }

    public interface IBounceDeformSpriteEffect
    {
        /// <summary> trueにするとエフェクトが動作する。 </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// エフェクトの強度を [0, 1] の範囲で指定する。1より大きい値も入れられるが、入れると動きがかなり極端になる。
        /// 初期値は0.5になっている。
        /// </summary>
        public float Intensity { get; set; }

        /// <summary>
        /// エフェクトの開始～終了までの周期。すでにエフェクトが起動しているときに書き換えると動きが飛ぶこともある。
        /// 初期値は0.5であり、値の範囲は [0.01, 5]
        /// </summary>
        public float Duration { get; set; }

        /// <summary>
        /// trueの場合、エフェクトが効き続ける。falseの場合、アクティブになってから1回ぶんの動作ののち、
        /// 自動で<see cref="IsActive"/>がfalseになる。初期値はtrue。
        /// </summary>
        public bool Loop { get; set; }
    }

    public interface IFloatingSpriteEffect
    {
        /// <summary> trueにするとエフェクトが動作する。 </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// 上昇幅を画面座標ベースで [0, 1] で指定する。目安としては0.05~0.15程度を指定すると無理なく動作する。
        /// 初期値は0.05
        /// </summary>
        public float Intensity { get; set; }

        /// <summary>
        /// エフェクトの開始～終了までの周期。すでにエフェクトが起動しているときに書き換えると動きが飛ぶこともある。
        /// 初期値は2であり、範囲は [0.1, 10]
        /// </summary>
        public float Duration { get; set; }

        // Floatはループしかせんやろ…ということでLoopオプションは省いている
    }
}
