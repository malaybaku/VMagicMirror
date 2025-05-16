namespace VMagicMirror.Buddy
{
    //TODO: manifest.jsonの定義方法をリンクさせたremarks docが欲しい
    /// <summary>
    /// 現在の姿勢やアバターの親ボーンのアタッチ先を読み取り可能なTransform情報です。
    /// とくに、マニフェストの定義に基づいて生成され、ユーザーがレイアウトを編集できる2DのTransform情報がこのinterfaceの値として表現されます。
    /// </summary>
    public interface IReadOnlyTransform2D
    {
        /// <summary>
        /// オブジェクトの現在のローカル位置を取得します。
        /// </summary>
        /// <remarks>
        /// このオブジェクトに親オブジェクトがない場合、このプロパティは <see cref="Position"/> と同じ値を返します。
        /// そうでない場合、親オブジェクトを基準としたローカル位置を返します。
        /// 基本的な座標系については <see cref="Position"/> の説明を参照してください。
        /// </remarks>
        Vector2 LocalPosition { get; }
        
        /// <summary>
        /// オブジェクトの現在のローカル回転を取得します。
        /// </summary>
        /// <remarks>
        /// このオブジェクトに親オブジェクトがない場合、このプロパティは <see cref="Rotation"/> と同じ値を返します。
        /// そうでない場合、親オブジェクトを基準としたローカル回転を返します。
        /// 回転の方向については <see cref="Rotation"/> の説明を参照してください。
        /// </remarks>
        Quaternion LocalRotation { get; }
        
        /// <summary>
        /// オブジェクトの画面上での位置を取得します。
        /// </summary>
        /// <remarks>
        /// この値は、アバターウィンドウのサイズをおおよそ1280x720として画面の左下を (0, 0) とした座標系で表現されます。
        /// xの値は画面の左端で 0 付近の値を取り、画面の右端で 1280 付近の値を取ります。
        /// yの値は画面の下端で 0 付近の値を取り、画面の上端で 720 付近の値を取ります。
        /// </remarks>
        Vector2 Position { get; }
        
        /// <summary>
        /// 回転を取得します。
        /// </summary>
        /// <remarks>
        /// 回転はそれぞれ以下の方向を表します。
        /// z軸まわりの回転は画面に対して平面的な回転です。
        /// x軸およびy軸まわりの回転は3次元的であり、このオブジェクトに <see cref="ISprite2D"/> が紐づく場合、その画像は遠近のついた表示になります。
        /// <list type="bullet">
        ///   <item>x軸: 画面の左右方向を軸とする回転</item>
        ///   <item>z軸: 画面の上下方向を軸とする回転</item>
        ///   <item>z軸: 画面と垂直な方向を軸とする回転</item>
        /// </list>
        /// </remarks>
        Quaternion Rotation { get; }

        /// <summary>
        /// オブジェクトのスケールを取得します。
        /// </summary>
        /// <remarks>
        /// このオブジェクトに親オブジェクトがある場合でも、親オブジェクトのスケールは考慮されません。
        /// </remarks>
        Vector2 LocalScale { get; }
        
        // NOTE: ChildrenとParentを追加してもいいかもしれないが、v4.0.0では導入してない
    }
}
