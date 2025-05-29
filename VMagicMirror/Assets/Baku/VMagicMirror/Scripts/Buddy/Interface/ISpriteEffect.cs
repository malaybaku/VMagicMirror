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
        /// ぷにぷにするような視覚効果を適用するエフェクトの設定を取得します。
        /// </summary>
        public IPuniSpriteEffect Puni { get; }
        
        /// <summary>
        /// 小刻みに振動するような視覚効果を適用するエフェクトの設定を取得します。
        /// </summary>
        public IVibrateSpriteEffect Vibrate { get; }

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
        /// スプライトの上昇幅を取得します。デフォルト値は 20 です。
        /// </summary>
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
    /// x軸に伸びながらy軸方向に縮む、またその逆を行うような、ぷにぷにする視覚効果を適用するエフェクトの設定です。
    /// </summary>
    public interface IPuniSpriteEffect
    {
        /// <summary>
        /// エフェクトを動作させるかどうかを取得、設定します。<c>true</c> を設定することでエフェクトが動作します。
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// エフェクトの強度を 0 以上の値として取得、設定します。デフォルト値は 0.1 です。
        /// </summary>
        /// <remarks>
        /// 値が 0 の場合、このエフェクトは動作しません。
        /// </remarks>
        public float Intensity { get; set; }

        /// <summary>
        /// エフェクトの開始～終了までの周期を秒単位で取得、設定します。値は [0.01, 5] の範囲で指定でき、デフォルト値は 0.5 です。
        /// </summary>
        /// <remarks>
        /// すでにエフェクトが起動しているときに書き換えると動きが不連続になることがあります。
        /// </remarks>
        public float Duration { get; set; }
    }

    /// <summary>
    /// x軸とy軸の各方向に振動するような視覚効果を適用するエフェクトの設定です。
    /// </summary>
    public interface IVibrateSpriteEffect
    {
        /// <summary>
        /// エフェクトを動作させるかどうかを取得、設定します。<c>true</c> を設定することでエフェクトが動作します。
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// 横方向の揺れ幅を取得、設定します。デフォルト値は 0 です。
        /// </summary>
        public float IntensityX { get; set; }

        /// <summary>
        /// 横方向の揺れの周波数を取得、設定します。デフォルト値は 20 です。
        /// </summary>
        public float FrequencyX { get; set; }

        /// <summary>
        /// 横方向の振動の位相オフセットを [0, 1] の範囲で取得、設定します。デフォルト値は 0 です。
        /// </summary>
        /// <remarks>
        /// x軸とy軸で振動のタイミングをずらしたいとき、このプロパティに値を指定します。
        /// </remarks>
        public float PhaseOffsetX { get; set; }
        
        /// <summary>
        /// 縦方向の揺れ幅を取得、設定します。デフォルト値は 0 です。
        /// </summary>
        public float IntensityY { get; set; }

        /// <summary>
        /// 縦方向の揺れの周波数を取得、設定します。デフォルト値は 20 です。
        /// </summary>
        public float FrequencyY { get; set; }
        
        /// <summary>
        /// 縦方向の振動の位相オフセットを [0, 1] の範囲で取得、設定します。デフォルト値は 0 です。
        /// </summary>
        /// <remarks>
        /// x軸とy軸で振動のタイミングをずらしたいとき、このプロパティに値を指定します。
        /// </remarks>
        public float PhaseOffsetY { get; set; }
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
        /// <param name="height">ジャンプの高さを指定します。この値は <see cref="ISprite2D.Size"/> を参考に指定します。</param>
        /// <param name="count">時間内に行うジャンプの回数</param>
        /// <remarks>
        ///   <paramref name="count"/> が2以上である場合、<paramref name="duration"/> の時間内に複数回のジャンプ動作を行います。
        ///   ジャンプの軌道は放物軌道として、指定したパラメータから自動で計算されます。
        /// </remarks>
        void Jump(float duration, float height, int count);

        /// <summary>
        /// ジャンプ動作を停止します。
        /// </summary>
        /// <remarks>
        /// この関数を呼び出すとスプライトの位置が不連続に移動することがあります。
        /// </remarks>
        void Stop();
    }
}
