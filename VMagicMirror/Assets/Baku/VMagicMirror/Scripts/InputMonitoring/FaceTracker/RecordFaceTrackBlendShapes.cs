namespace Baku.VMagicMirror
{
    /// <summary>
    /// パーフェクトシンクで定義しているのとほぼ同等のブレンドシェイプ一覧を取得できるようなAPIを定義します。
    /// iFacialMocap、MediaPipeなど、一連の52個前後のブレンドシェイプが検出できるようなトラッキングシステムのラッパーはこのインターフェイスを実装します。
    /// </summary>
    public interface IFaceTrackBlendShapes
    {
        EyeBlendShape Eye { get; }
        BrowBlendShape Brow { get; }
        MouthBlendShape Mouth { get; }
        JawBlendShape Jaw { get; }
        CheekBlendShape Cheek { get; }
        NoseBlendShape Nose { get; }
        TongueBlendShape Tongue { get; }
    }

    /// <summary>
    /// 単にレコード型ライクに実装された <see cref="IFaceTrackBlendShapes"/> のデフォルト実装です。
    /// </summary>
    public class RecordFaceTrackBlendShapes : IFaceTrackBlendShapes
    {
        public EyeBlendShape Eye { get; } = new();
        public BrowBlendShape Brow { get; } = new();
        public MouthBlendShape Mouth { get; } = new();
        public JawBlendShape Jaw { get; } = new();
        public CheekBlendShape Cheek { get; } = new();
        public NoseBlendShape Nose { get; } = new();
        public TongueBlendShape Tongue { get; } = new();
    }    
      
    // NOTE: I/Fにはgetterだけ公開してsetterは実装だけが使えるような建付けだとキレイだが、オーバーエンジニアリングっぽいので無し
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