namespace VMagicMirror.Buddy
{
    // NOTE: エフェクト設定はAPI上では数値の集まりに見えるだけの値なため、インターフェースではなく具象クラスとして定義されている
    
    /// <summary>
    /// スプライトを移動・回転・変形できるようなエフェクトを提供するAPIです。
    /// </summary>
    /// <remarks>
    /// <para>
    ///  いずれのエフェクトもデフォルトでは無効になっています。
    ///  <see cref="IFloatingSpriteEffect.IsActive"/> などで <c>IsActive</c> プロパティを有効にするか、
    ///  または<see cref="IJumpSpriteEffect.Jump"/> のように動作を開始させるメソッドを呼び出すことでエフェクトを適用します。
    /// </para>
    /// <para>
    ///  一部のエフェクト設定値には値の範囲、下限、上限が定められています。
    ///  これらの値については、範囲を超えた値は丸めて適用されます。
    /// </para>
    /// 
    /// </remarks>
    public interface ISpriteEffect
    {
        /// <summary>
        /// 浮遊しているような視覚効果を適用するエフェクトの設定を取得します。
        /// </summary>
        public IFloatingSpriteEffect Floating { get; }

        /// <summary>
        /// 跳ねているような視覚効果を適用するエフェクトの設定を取得します。
        /// </summary>
        public IBounceDeformSpriteEffect BounceDeform { get; }
        
        /// <summary>
        /// ジャンプ動作を適用するエフェクトの設定を取得します。
        /// </summary>
        public IJumpSpriteEffect Jump { get; }
    }

    /// <summary>
    /// 浮遊しているような視覚効果を適用するエフェクトの設定です。
    /// </summary>
    public interface IFloatingSpriteEffect
    {
        /// <summary>
        /// エフェクトを動作させるかどうかを取得、設定します。<c>true</c> を設定することでエフェクトが動作します。
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// スプライトの上昇幅を画面座標ベースで取得、設定します。値は [0, 1] の範囲で指定でき、デフォルト値は0.05です。
        /// </summary>
        /// <remarks>
        /// 目安として [0.05, 0.15] 程度を指定すると無理なく動作します。
        /// </remarks>
        public float Intensity { get; set; }

        /// <summary>
        /// エフェクトの開始～終了までの周期を取得、設定します。0.1 以上の値が指定でき、デフォルト値は 5 です。
        /// </summary>
        /// <remarks>
        /// すでにエフェクトが起動しているときに書き換えると動きが不連続になることがあります。
        /// </remarks>
        public float Duration { get; set; }

        // NOTE: Floatingは単発動作させる理由がなさそうなため、Loopオプションは定義されない
    }

    /// <summary>
    /// 跳ねているような視覚効果を適用するエフェクトの設定です。
    /// </summary>
    public interface IBounceDeformSpriteEffect
    {
        /// <summary>
        /// エフェクトを動作させるかどうかを取得、設定します。<c>true</c> を設定することでエフェクトが動作します。
        /// </summary>
        /// <remarks>
        /// この値はスクリプトから直接更新することで変化する以外に、
        /// <see cref="Loop"/> が <c>false</c> である場合には自動で <c>true</c> から <c>false</c> に切り替わります。
        /// </remarks>
        public bool IsActive { get; set; }

        /// <summary>
        /// エフェクトの強度を取得、設定します。値は [0, 1] の範囲で指定でき、デフォルト値は 0.5 です。
        /// </summary>
        /// <remarks>
        /// 1 より大きい値も指定できますが、その場合は挙動が非常に極端になります。
        /// </remarks>
        public float Intensity { get; set; }

        /// <summary>
        /// エフェクトの開始～終了までの周期を秒単位で取得、設定します。値は [0.01, 5] の範囲で指定でき、デフォルト値は 0.5 です。
        /// </summary>
        /// <remarks>
        /// すでにエフェクトが起動しているときに書き換えると動きが不連続になることがあります。
        /// </remarks>
        public float Duration { get; set; }

        /// <summary>
        /// エフェクトをループ実行するかどうかを取得、設定します。デフォルト値は<c>true</c>です。
        /// </summary>
        /// <remarks>
        /// この値が<c>true</c>の場合、エフェクトは<see cref="IsActive"/>をオフにするまで動作し続けます。
        /// この値が<c>false</c>の場合、エフェクトが1周期ぶん動作すると自動で<see cref="IsActive"/>が<c>false</c>に切り替わります。
        /// </remarks>
        public bool Loop { get; set; }
    }

    /// <summary>
    /// ジャンプ動作を適用するエフェクトの設定です。
    /// </summary>
    public interface IJumpSpriteEffect
    {
        /// <summary>
        /// ジャンプ動作を行います。
        /// </summary>
        /// <param name="duration">動作を行う時間を秒数として指定します。</param>
        /// <param name="intensity">ジャンプの高さを、画面座標で指定します。値の範囲は [0, 1] です。</param>
        /// <param name="count">時間内に行うジャンプの回数</param>
        /// <remarks>
        ///   <paramref name="count"/> が2以上である場合、<paramref name="duration"/> の時間内に複数回のジャンプ動作を行います。
        ///   ジャンプの軌道は放物軌道として、パラメータから自動で計算されます。
        /// </remarks>
        void Jump(float duration, float intensity, int count);

        /// <summary>
        /// ジャンプ動作を停止します。
        /// </summary>
        /// <remarks>
        /// この関数を呼び出すとスプライトの位置が不連続に移動することがあります。
        /// </remarks>
        void Stop();
    }
}
