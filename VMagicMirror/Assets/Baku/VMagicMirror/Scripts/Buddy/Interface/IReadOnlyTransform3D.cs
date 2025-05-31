namespace VMagicMirror.Buddy
{
    //TODO: manifest.jsonの定義方法をリンクさせたremarks docが欲しい
    /// <summary>
    /// 現在の姿勢やアバターの親ボーンのアタッチ先を読み取り可能なTransform情報です。
    /// とくに、マニフェストの定義に基づいて生成され、ユーザーがレイアウトを編集できる3DのTransform情報がこのinterfaceの値として表現されます。
    /// </summary>
    /// <remarks>
    /// <para>
    ///   このTransformの配置先は <see cref="AttachedBone"/> プロパティの値によって変化します。
    ///   <see cref="AttachedBone"/> が <see cref="HumanBodyBones.None"/> である場合、またはアバターがロードされていない場合、
    ///   Transformは空間上に直接配置されており、親オブジェクトはありません。
    /// </para>
    /// <para>
    ///   <see cref="AttachedBone"/> が <see cref="HumanBodyBones.None"/> 以外であり、かつアバターがロード済みの場合、このTransformは指定されたボーンの子要素となります。
    ///   <see cref="AttachedBone"/> が任意ボーンであり、かつボーンが存在しなかった場合、このTransformは任意ボーンの親になるような有効なボーンの子要素となります。
    /// </para>
    /// </remarks>
    public interface IReadOnlyTransform3D
    {
        // NOTE: Position/RotationはAttachedBoneに対してのローカル姿勢を表すことに注意
        
        /// <summary>
        /// Transformの位置をローカル座標の値として取得します。
        /// </summary>
        /// <remarks>
        /// ローカル座標の基準となる親オブジェクトの有無は <see cref="AttachedBone"/> の値によって変化します。
        /// 詳しくは <see cref="IReadOnlyTransform3D"/> の説明を参照してください。
        /// </remarks>
        Vector3 LocalPosition { get; }
        
        /// <summary>
        /// Transformの回転をローカル座標の値として取得します。
        /// </summary>
        /// <remarks>
        /// ローカル座標の基準となる親オブジェクトの有無は <see cref="AttachedBone"/> の値によって変化します。
        /// 詳しくは <see cref="IReadOnlyTransform3D"/> の説明を参照してください。
        /// </remarks>
        Quaternion LocalRotation { get; }
        
        /// <summary>
        /// Transformの位置をワールド座標の値として取得します。
        /// </summary>
        Vector3 Position { get; }

        /// <summary>
        /// Transformの回転をワールド座標の値として取得します。
        /// </summary>
        Quaternion Rotation { get; }
        
        /// <summary>
        /// ローカルスケールを取得します。
        /// </summary>
        Vector3 LocalScale { get; }

        /// <summary>
        /// アバターに対して、このTransformをアタッチするボーンを取得します。
        /// </summary>
        /// <remarks>
        /// <para>
        ///   Transformをアバターにアタッチしないように設定されている場合、このプロパティは <see cref="HumanBodyBones.None"/> を返します。
        /// </para>
        /// <para>
        ///   この値が任意ボーンを指しており、かつアバターにそのボーンがない場合、実際には親ボーンに対してTransformがアタッチされます。
        ///   詳しくは <see cref="IReadOnlyTransform3D"/> の説明を参照してください。
        /// </para>
        /// </remarks>
        HumanBodyBones AttachedBone { get; }
    }
}
