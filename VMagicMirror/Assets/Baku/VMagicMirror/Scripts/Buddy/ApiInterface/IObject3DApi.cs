namespace Baku.VMagicMirror.Buddy.Api.Interface
{
    /// <summary>
    /// <see cref="ISprite3DApi"/>, <see cref="IGlbApi"/>, <see cref="IVrmApi"/>で利用可能な、
    /// 3Dオブジェクト用の共通の機能を定義します。
    /// </summary>
    public interface IObject3DApi
    {
        Vector3 LocalPosition { get; set; }
        Quaternion LocalRotation { get; set; }
        Vector3 LocalScale { get; set; }

        // ワールド座標のGet/Set API. とくに、Setについては次のフレームで(親のTransform3DApiが移動するなどで)結果が保持されるとは限らないことに注意
        public Vector3 GetPosition();
        public Quaternion GetRotation();
        void SetPosition(Vector3 position);
        void SetRotation(Quaternion rotation);

        void SetParent(ITransform3DApi parent);
        
        void Hide();
    }
}
