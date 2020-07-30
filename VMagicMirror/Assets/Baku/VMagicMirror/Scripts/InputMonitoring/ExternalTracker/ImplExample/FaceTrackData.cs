using System;

namespace Baku.VMagicMirror.ExternalTracker.ImplExample
{
    /// <summary>
    /// 顔トラッキングデータのメタ情報。
    /// データ本体のシリアライズ前にこの値を読み出すことでパースの続行有無とか、パース手段の切り替えを行う。
    /// </summary>
    [Serializable]
    public class FaceTrackMetaData
    {
        public FaceTrackMetaDataContent metadata;
    }

    [Serializable]
    public class FaceTrackMetaDataContent
    {
        public string target;
        public string version;
    }

    //NOTE: ここから下について、face_tranform以外は
    //「-1なら無効、0~65535ならそれを65535.0で割った値がブレンドシェイプ値であるようなブレンドシェイプ値」です。
    
    /// <summary>
    /// 顔トラッキングで貰えるはずの情報一覧です。VRM的には過剰な情報が飛んでくるので一部は捨てます。
    /// </summary>
    [Serializable]
    public class FaceTrackData
    {
        /// <summary> 顔をロスしたよ、という通信を明確に投げてくれる親切なフラグ(あると嬉しい…という気持ちでプロトコルに入れてます) </summary>
        public bool is_lost;
        public FaceTransformInfo face_transform;
        public EyeBlendShapeInfo eye;
        public BrowBlendShapeInfo brow;
        public MouthBlendShapeInfo mouth;
        public JawBlendShapeInfo jaw;
        public CheekBlendShapeInfo cheek;
        public NoseBlendShapeInfo nose;
        public TongueBlendShapeInfo tongue;
    }

    [Serializable]
    public struct EyeBlendShapeInfo
    {
        //NOTE: look_inは寄り目、look_outは離し目のこと。
        //つまり左右の目でlook_inのさす方向が真逆なので要注意。

        public int l_blink;
        public int l_look_up;
        public int l_look_down;
        public int l_look_in;
        public int l_look_out;
        public int l_wide;
        public int l_squint;
        
        public int r_blink;
        public int r_look_up;
        public int r_look_down;
        public int r_look_in;
        public int r_look_out;
        public int r_wide;
        public int r_squint;
    }

    [Serializable]
    public struct FaceTransformInfo
    {
        //NOTE: たしか右手系だったような
        public float pos_x;
        public float pos_y;
        public float pos_z;

        //NOTE: radianです
        public float rot_angle;

        //NOTE: コレも右手系のはず。Quaternion作るときに注意しましょう
        public float rot_axis_x;
        public float rot_axis_y;
        public float rot_axis_z;
    }

    [Serializable]
    public struct JawBlendShapeInfo
    {
        public int open;
        public int forward;
        public int right;
        public int left;
    }

    [Serializable]
    public struct MouthBlendShapeInfo
    {
        public int close;
        public int funnel;
        public int pucker;
        
        public int right;
        public int left;

        public int shrug_upper;
        public int shrug_lower;
        public int roll_upper;
        public int roll_lower;
        
        public int l_smile;
        public int l_frown;
        public int l_press;
        public int l_dimple;
        public int l_stretch;
        public int l_lowerDown;
        public int l_upperUp;
        
        public int r_smile;
        public int r_frown;
        public int r_press;
        public int r_dimple;
        public int r_stretch;
        public int r_lowerDown;
        public int r_upperUp;
    }
    
    [Serializable]
    public struct NoseBlendShapeInfo
    {
        public int l_sneer;
        public int r_sneer;
    }

    [Serializable]
    public struct CheekBlendShapeInfo
    {
        public int puff;
        public int r_squint;
        public int l_squint;
    }
    
    [Serializable]
    public struct BrowBlendShapeInfo
    {
        public int r_down;
        public int r_outer_up;

        public int l_down;
        public int l_outer_up;
        
        public int inner_up;
    }
    
    
    [Serializable]
    public struct TongueBlendShapeInfo
    {
        //NOTE: outじゃないのは単に"out"だとC#の予約語になるから。
        public int tongueOut;
    }
    
}
