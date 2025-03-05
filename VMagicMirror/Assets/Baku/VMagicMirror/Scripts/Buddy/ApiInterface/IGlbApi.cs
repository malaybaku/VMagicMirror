namespace Baku.VMagicMirror.Buddy.Api.Interface
{
    /// <summary>
    /// GLBファイルを読み込めるような3Dオブジェクトです。
    /// </summary>
    public interface IGlbApi : IObject3DApi
    {
        // TODO: 実装都合でタスク化するのも検討してよい
        
        /// <summary>
        /// ファイルパスを指定して3Dデータをロードし、表示します。
        /// </summary>
        /// <param name="path">GLB</param>
        void Load(string path);
        
        /// <summary>
        /// オブジェクトを表示します。<see cref="Hide"/> 関数で
        /// </summary>
        void Show();

        /// <summary>
        /// 読み込んだGLBデータに定義されたアニメーションの名称を取得します。
        /// </summary>
        /// <returns>アニメーション名の一覧</returns>
        /// <remarks>
        /// <see cref="Load"/>よりも前にこの関数を呼び出した場合や、GLBデータにアニメーションが定義されていない場合は空の配列を取得します。
        /// </remarks>
        string[] GetAnimationNames();
        
        // NOTE: 遷移方法を指定する感じの引数も欲しくなりそう

        /// <summary>
        /// 名前を指定してアニメーションを実行します。
        /// </summary>
        /// <param name="name">アニメーション名</param>
        /// <param name="isLoop">ループ再生を行うかどうか</param>
        /// <remarks>
        /// <para>
        ///  この関数で開始させたアニメーションは、アニメーションの実行完了、または別のアニメーションの開始、
        ///  または <see cref="StopAnimation"/> の呼び出しによって停止します。
        /// </para>
        /// <para>
        ///  読み込んだGLBデータに定義されていないアニメーション名を指定して呼び出した場合は何も起こりません。
        /// </para>
        /// </remarks>
        void RunAnimation(string name, bool isLoop);

        /// <summary>
        /// <see cref="RunAnimation"/> で実行したアニメーションを停止します。
        /// </summary>
        /// <remarks>
        /// アニメーションがすでに停止している場合、この関数を呼んでもとくに何も起こりません。
        /// この関数でアニメーションを停止させると、オブジェクトの見た目が不連続に変化することがあります。
        /// </remarks>
        void StopAnimation();

        /// <summary>
        /// オブジェクトを非表示にします。
        /// </summary>
        void Hide();

        //TODO: エフェクト (サイズキープしてぷにぷにするとか) も欲しい
    }
}
