namespace VMagicMirror.Buddy
{
    /// <summary>
    /// 姿勢を取得・編集できるような3DオブジェクトのTransform情報です。
    /// </summary>
    public interface ITransform3D
    {
        /// <summary>
        /// インスタンスを読み取り専用とみなした値に変換します。
        /// </summary>
        /// <returns>読み取り専用扱いに変換した値</returns>
        /// <remarks>
        /// このメソッドの戻り値を経由するとTransformの状態は編集できなくなります。
        /// ただし、呼び出し元のインスタンス自体は引き続き編集可能です。
        /// </remarks>
        IReadOnlyTransform3D AsReadOnly();
        
        /// <summary>
        /// オブジェクトのローカル座標での位置を取得、設定します。
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see cref="SetParent(IReadOnlyTransform3D)"/> などで親オブジェクトを指定している場合、この値は親オブジェクトに対するローカル姿勢を表します。
        /// そうでない場合、この値はワールド座標に相当する値になります。
        /// </para>
        /// </remarks>
        Vector3 LocalPosition { get; set; }
    
        /// <summary>
        /// オブジェクトのローカル座標での回転を取得、設定します。
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see cref="SetParent(IReadOnlyTransform3D)"/> などで親オブジェクトを指定している場合、この値は親オブジェクトに対するローカル姿勢を表します。
        /// そうでない場合、この値はワールド座標に相当する値になります。
        /// </para>
        /// </remarks>
        Quaternion LocalRotation { get; set; }

        /// <summary>
        /// オブジェクトのローカル座標でのスケールを取得、設定します。
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see cref="SetParent(IReadOnlyTransform3D)"/> などで親オブジェクトを指定している場合、この値は親オブジェクトのスケールと組み合わせて適用されます。
        /// そうでない場合、この値そのものでオブジェクトのスケールが定まります。
        /// </para>
        /// </remarks>
        Vector3 LocalScale { get; set; }

        /// <summary>
        /// オブジェクトの位置をワールド座標で取得、設定します。
        /// </summary>
        Vector3 Position { get; set; }

        /// <summary>
        /// オブジェクトの回転をワールド座標で取得、設定します。
        /// </summary>
        Quaternion Rotation { get; set; }
        
        /// <summary>
        /// 親オブジェクトを指定します。
        /// </summary>
        /// <param name="parent">親オブジェクト</param>
        /// <remarks>
        /// 親オブジェクトを指定すると、このTransformが関連するオブジェクトの姿勢や大きさは親オブジェクトの影響を受けるようになります。
        /// </remarks>
        void SetParent(IReadOnlyTransform3D parent);

        /// <inheritdoc cref="SetParent(IReadOnlyTransform3D)"/>
        void SetParent(ITransform3D parent);

        /// <summary>
        /// 親オブジェクトとして、メインアバターの特定のボーンを指定します。
        /// </summary>
        /// <param name="bone">親オブジェクトとして用いるアバターのボーン</param>
        /// <remarks>
        /// <see cref="HumanBodyBones.None"/>や<see cref="HumanBodyBones.LastBone"/>を指定した場合、親オブジェクトを外す処理として扱われます。
        /// この関数はメインアバターがロードされていない状態でも呼び出せます。
        /// メインアバターがロードされた時点で、指定したアバターのボーンが親オブジェクトに切り替わります。
        /// </remarks>
        void SetParent(HumanBodyBones bone);

        /// <summary>
        /// 親オブジェクトを削除し、オブジェクトをワールド空間に直接配置した状態にします。
        /// </summary>
        void RemoveParent();
    }
}
