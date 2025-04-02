using UnityEngine;

namespace Baku.VMagicMirror.ExternalTracker
{
    /// <summary>
    /// 顔トラッキングの情報元として提供すべきデータの一覧を定義します。
    /// </summary>
    /// <remarks>
    /// プロパティ側(FaceTransformとか)がI/Fでないのは「そこまでしないでもいいかな…」という判断に基づきます。
    /// </remarks>
    public interface IFaceTrackSource : IFaceTrackBlendShapes
    {
        /// <summary>
        /// NOTE: データ送信元が「顔をロストした」というデータを明確に送ってくれる場合、ここのflagを折ることでロストを表現します。
        /// (残念ながら)全アプリが対応してる機能ではないだろうな…というのが現時点で世相を見回した所感です。
        /// </summary>
        bool IsLost { get; }
        
        FaceTransform FaceTransform { get; }
    }

    /// <summary>
    /// 単にレコード型ライクに実装されたIFaceTrackSourceの実装です。普通コレで用が足ります。
    /// </summary>
    public class RecordFaceTrackSource : IFaceTrackSource
    {
        public bool IsLost { get; set; } = false;
        
        public FaceTransform FaceTransform { get; } = new FaceTransform();
        public EyeBlendShape Eye { get; } = new EyeBlendShape();
        public BrowBlendShape Brow { get; } = new BrowBlendShape();
        public MouthBlendShape Mouth { get; } = new MouthBlendShape();
        public JawBlendShape Jaw { get; } = new JawBlendShape();
        public CheekBlendShape Cheek { get; } = new CheekBlendShape();
        public NoseBlendShape Nose { get; } = new NoseBlendShape();
        public TongueBlendShape Tongue { get; } = new TongueBlendShape();
    }
    
    public class FaceTransform
    {
        /// <summary><see cref="Position"/>がセンシングされた値かどうかを取得、設定します。</summary>
        /// <remarks>
        /// 技術的にはデバイスからみた顔の座標が送れてすごーく便利なんだけど、アプリによって値を拾ってたり拾ってなかったりするので。
        /// コレがfalseの場合、上位側のクラスがPositionの代わりっぽい値を生成する権利が生じます
        /// </remarks>
        public bool HasValidPosition { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
    }
}
