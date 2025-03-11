namespace VMagicMirror.Buddy
{
    //TODO: manifest.jsonの定義方法をリンクさせたremarks docが欲しい
    /// <summary>
    /// 現在の姿勢やアバターの親ボーンのアタッチ先を読み取り可能なTransform情報です。
    /// とくに、マニフェストの定義に基づいて生成され、ユーザーがレイアウトを編集できる2DのTransform情報がこのinterfaceの値として表現されます。
    /// </summary>
    public interface IReadOnlyTransform2D
    {
        //TODO: doc commentを書く
        Vector2 LocalPosition { get; }
        Quaternion LocalRotation { get; }
        
        /// <summary>
        /// 画面上での位置を取得します。
        /// </summary>
        Vector2 Position { get; }
        
        /// <summary>
        /// 回転を取得します。
        /// </summary>
        /// <remarks>
        /// とくにz軸まわりの回転が、奥行き方向への変更を伴わない回転を表します。
        /// 通常、この値はz軸回りのみの回転を表した値になります。
        /// ただし、計算誤差などによってz軸以外の微小な回転を含む値を返すことがあります。
        /// </remarks>
        Quaternion Rotation { get; }

        /// <summary>
        /// スケールを取得します。
        /// </summary>
        Vector2 LocalScale { get; }
    }
}
