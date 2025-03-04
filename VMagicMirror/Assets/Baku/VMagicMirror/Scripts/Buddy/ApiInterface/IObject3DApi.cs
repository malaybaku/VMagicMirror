namespace Baku.VMagicMirror.Buddy.Api.Interface
{
    //TODO: バラして個別のAPIに直接定義したい vs. 単にObject3Dという汎用APIがあることにする手もある
    
    /// <summary>
    /// <see cref="ISprite3DApi"/>, <see cref="IGlbApi"/>, <see cref="IVrmApi"/>で利用可能な、
    /// 3Dオブジェクト用の共通の機能を定義します。
    /// </summary>
    public interface IObject3DApi
    {
        /// <summary>
        /// オブジェクトのローカル位置を取得、設定します。
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see cref="SetParent"/>関数で親オブジェクトを指定している場合、この値は親オブジェクトに対するローカル姿勢を表します。
        /// そうでない場合、この値はワールド座標に相当する値になります。
        /// </para>
        /// </remarks>
        Vector3 LocalPosition { get; set; }
    
        /// <summary>
        /// オブジェクトのローカル回転を取得、設定します。
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see cref="SetParent"/>関数で親オブジェクトを指定している場合、この値は親オブジェクトに対するローカル姿勢を表します。
        /// そうでない場合、この値はワールド座標に相当する値になります。
        /// </para>
        /// </remarks>
        Quaternion LocalRotation { get; set; }

        /// <summary>
        /// オブジェクトのローカルスケールを取得、設定します。
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see cref="SetParent"/>関数で親オブジェクトを指定している場合、この値は親オブジェクトのスケールと組み合わせて適用されます。
        /// そうでない場合、この値そのものでオブジェクトのスケールが定まります。
        /// </para>
        /// </remarks>
        Vector3 LocalScale { get; set; }

        /// <summary>
        /// オブジェクトの位置をワールド座標で取得します。
        /// </summary>
        /// <returns>オブジェクトの位置</returns>
        public Vector3 GetPosition();
        /// <summary>
        /// オブジェクトの回転をワールド座標で取得します。
        /// </summary>
        /// <returns>オブジェクトの回転</returns>
        public Quaternion GetRotation();

        /// <summary>
        /// オブジェクトの位置をワールド座標で設定します。
        /// </summary>
        /// <param name="position">オブジェクトの位置</param>
        /// <para>
        ///   <see cref="SetParent"/>で親オブジェクトを指定した状態でこの関数を呼び出した場合、
        ///   次フレーム以降で指定した位置からオブジェクトが移動する場合があります。
        /// </para>
        void SetPosition(Vector3 position);

        /// <summary>
        /// オブジェクトの回転をワールド座標で設定します。
        /// </summary>
        /// <param name="rotation">オブジェクトの回転</param>
        /// <para>
        ///   <see cref="SetParent"/>で親オブジェクトを指定した状態でこの関数を呼び出した場合、
        ///   次フレーム以降で指定した回転からオブジェクトが回転する場合があります。
        /// </para>
        void SetRotation(Quaternion rotation);

        /// <summary>
        /// 親オブジェクトを指定します。
        /// </summary>
        /// <param name="parent">親オブジェクト</param>
        /// <remarks>
        ///   この関数を呼び出すことで、指定した<paramref name="parent"/>をユーザーが設定から編集してオブジェクトの基本的な姿勢、サイズなどを編集できるようになります。
        ///   多くのケースで、ユーザーにレイアウトの編集サポートを提供するためにこの関数を呼び出すことが適しています。
        /// </remarks>
        void SetParent(ITransform3DApi parent);
        
        /// <summary>
        /// オブジェクトを非表示にします。
        /// </summary>
        void Hide();
    }
}
