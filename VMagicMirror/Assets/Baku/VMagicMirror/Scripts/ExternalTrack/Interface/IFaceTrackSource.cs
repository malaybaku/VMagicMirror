using UnityEngine;

namespace Baku.VMagicMirror.ExternalTracker
{
    /// <summary>
    /// 顔トラッキングの情報元として提供すべきデータの一覧を定義します。
    /// </summary>
    /// <remarks>
    /// プロパティ側(FaceTransformとか)がI/Fでないのは「そこまでしないでもいいかな…」という判断に基づきます。
    /// </remarks>
    public interface IFaceTrackSource
    {
        /// <summary>
        /// NOTE: データ送信元が「顔をロストした」というデータを明確に送ってくれる場合、ここのflagを折ることでロストを表現します。
        /// (残念ながら)全アプリが対応してる機能ではないだろうな…というのが現時点で世相を見回した所感です。
        /// </summary>
        bool IsLost { get; }
        
        FaceTransform FaceTransform { get; }
        EyeBlendShape Eye { get; }
        BrowBlendShape Brow { get; }
        MouthBlendShape Mouth { get; }
        JawBlendShape Jaw { get; }
        CheekBlendShape Cheek { get; }
        NoseBlendShape Nose { get; }
        TongueBlendShape Tongue { get; }
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
    
    public class EyeBlendShape
    {
        //NOTE: look_inは寄り目、look_outは離し目のこと。
        //つまり左右の目でlook_inのさす方向が真逆なので要注意。

        public float LeftBlink { get; set; }
        public float LeftLookUp { get; set; }
        public float LeftLookDown { get; set; }
        public float LeftLookIn { get; set; }
        public float LeftLookOut { get; set; }
        public float LeftWide { get; set; }
        public float LeftSquint { get; set; }
        
        public float RightBlink { get; set; }
        public float RightLookUp { get; set; }
        public float RightLookDown { get; set; }
        public float RightLookIn { get; set; }
        public float RightLookOut { get; set; }
        public float RightWide { get; set; }
        public float RightSquint { get; set; }
        
    }
    
    public class JawBlendShape
    {
        public float Open { get; set; }
        public float Forward { get; set; }
        public float Right { get; set; }
        public float Left { get; set; }
    }

    public class MouthBlendShape
    {
        public float Close { get; set; }
        public float Funnel { get; set; }
        public float Pucker { get; set; }
        
        public float Right { get; set; }
        public float Left { get; set; }

        public float ShrugUpper { get; set; }
        public float ShrugLower { get; set; }
        public float RollUpper { get; set; }
        public float RollLower { get; set; }
        
        public float LeftSmile { get; set; }
        public float LeftFrown { get; set; }
        public float LeftPress { get; set; }
        public float LeftDimple { get; set; }
        public float LeftStretch { get; set; }
        public float LeftLowerDown { get; set; }
        public float LeftUpperUp { get; set; }
        
        public float RightSmile { get; set; }
        public float RightFrown { get; set; }
        public float RightPress { get; set; }
        public float RightDimple { get; set; }
        public float RightStretch { get; set; }
        public float RightLowerDown { get; set; }
        public float RightUpperUp { get; set; }
        
    }
    
    public class NoseBlendShape
    {
        public float LeftSneer { get; set; }
        public float RightSneer { get; set; }
    }

    public class CheekBlendShape
    {
        public float Puff { get; set; }
        public float RightSquint { get; set; }
        public float LeftSquint { get; set; }
    }
    
    public class BrowBlendShape
    {
        public float RightDown { get; set; }
        public float RightOuterUp { get; set; }

        public float LeftDown { get; set; }
        public float LeftOuterUp { get; set; }
        
        public float InnerUp { get; set; }
    }
    
    public class TongueBlendShape
    {
        public float TongueOut { get; set; }
    }
}
