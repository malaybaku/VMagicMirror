using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Baku.VMagicMirror.MotionExporter
{
    public static class SerializeKeyframeParams
    {
        //NOTE: 暗黙にUnityのWeightedModeもこの値(Unity 2019.4時点)だが、更新の影響受けると嫌なので自前定義する
        public const int WeightedModeNone = 0;
        public const int WeightedModeIn = 1;
        public const int WeightedModeOut = 2;
        public const int WeightedModeBoth = 3;

        public static int GetInt(WeightedMode mode)
        {
            switch (mode)
            {
            case WeightedMode.None: return WeightedModeNone;
            case WeightedMode.In: return WeightedModeIn;
            case WeightedMode.Out: return WeightedModeOut;
            case WeightedMode.Both: return WeightedModeBoth;
            //普通来ない
            default: return WeightedModeNone;
            }
        }

        public static WeightedMode GetWeighedMode(int mode)
        {
            switch (mode)
            {
            case WeightedModeNone: return WeightedMode.None;
            case WeightedModeIn: return WeightedMode.In;
            case WeightedModeOut: return WeightedMode.Out;
            case WeightedModeBoth: return WeightedMode.Both;
            //普通来ない
            default: return WeightedMode.None;
            }
        }
    }
    
    /// <summary> VRMなHumanoidアニメーションのシリアライズ化データ。AnimationClipに相当。 </summary>
    [Serializable]
    public class SerializedMotion
    {
        /// <summary> ExportしたときのMotionExporterのライブラリバージョン。あとで後方互換しやすいように。 </summary>
        public string version;
        
        public List<SerializedMotionCurveBinding> curveBindings;
        
        /// <summary>
        /// どのマッスルがアニメーション対象となっているかのフラグ一覧を取得します。
        /// </summary>
        /// <returns></returns>
        public bool[] LoadMuscleFlags()
        {
            return Enumerable.Range(0, 95)
                .Select(i => curveBindings.Any(b => b.propertyName == $"p{i}"))
                .ToArray();
        }
    }
    
    /// <summary> VRM用のHumanoidアニメーションで、特定プロパティ向けのカーブをメタ情報とセットで保持する。EditorCurveBindingに相当 </summary>
    [Serializable]
    public class SerializedMotionCurveBinding
    {
        public string path;
        public string propertyName;
        public string typeName;
        public bool isDiscreteCurve;
        public bool isPPtrCurve;
        public SerializedMotionCurve curve;
    }

    /// <summary> VRM用Humanoidアニメーションで、カーブ情報を保持する。AnimationCurveに相当</summary>
    [Serializable]
    public class SerializedMotionCurve
    {
        //SerializedMotionKeyFrameをバイナリ配列に詰め込んでb64エンコードした文字列
        //普通にJSON化すると長さが4~5倍に伸びてしまう+全体に占める比率がでかいのでこうしてます
        public string b64KeyFrames;
        
        //NOTE: KeyFramesの文字列は長くなりがちなため、あんまりアレだったらバイナリのb64化する
        //public List<SerializedMotionKeyFrame> Keyframes;
    }
    
    /// <summary> VRM用Humanoidアニメーションで、カーブ情報のキーフレームを保持する。KeyFrameに相当</summary>
    /// <remarks>
    /// JSONがあまり長くならないようフィールド名を縮めたり精度を削ったりしてます
    /// </remarks>
    public class SerializedMotionKeyFrame
    {
        public float time;
        public float value;

        public float inTangent;
        public float inWeight;

        public float outTangent;
        public float outWeight;

        public int weightedMode;

        //4byte * 7メンバなので。
        public const int BinaryCount = 28;

        //NOTE: KeyframeにはtangentModeというのもあるんだけど、Editor限定の値だと判断して無視してます
    }

    /// <summary>
    /// Unity 2019.4におけるHumanoid AnimationのデータがほぼMuscle情報になってる事を踏まえ(※ほんとはIKがあるんだけど)、
    /// その値をただのプロパティとして受けられるように読み替えるべくプロパティ名をいじるマッパー
    /// </summary>
    public static class SerializeMuscleNameMapper
    {
        private static readonly Dictionary<string, string> _propNameRenames = new Dictionary<string, string>
        {
            ["Spine Front-Back"] = "p0",
            ["Spine Left-Right"] = "p1",
            ["Spine Twist Left-Right"] = "p2",
            ["Chest Front-Back"] = "p3",
            ["Chest Left-Right"] = "p4",
            ["Chest Twist Left-Right"] = "p5",
            ["UpperChest Front-Back"] = "p6",
            ["UpperChest Left-Right"] = "p7",
            ["UpperChest Twist Left-Right"] = "p8",
            ["Neck Nod Down-Up"] = "p9",
            ["Neck Tilt Left-Right"] = "p10",
            ["Neck Turn Left-Right"] = "p11",
            ["Head Nod Down-Up"] = "p12",
            ["Head Tilt Left-Right"] = "p13",
            ["Head Turn Left-Right"] = "p14",
            ["Left Eye Down-Up"] = "p15",
            ["Left Eye In-Out"] = "p16",
            ["Right Eye Down-Up"] = "p17",
            ["Right Eye In-Out"] = "p18",
            ["Jaw Close"] = "p19",
            ["Jaw Left-Right"] = "p20",
            ["Left Upper Leg Front-Back"] = "p21",
            ["Left Upper Leg In-Out"] = "p22",
            ["Left Upper Leg Twist In-Out"] = "p23",
            ["Left Lower Leg Stretch"] = "p24",
            ["Left Lower Leg Twist In-Out"] = "p25",
            ["Left Foot Up-Down"] = "p26",
            ["Left Foot Twist In-Out"] = "p27",
            ["Left Toes Up-Down"] = "p28",
            ["Right Upper Leg Front-Back"] = "p29",
            ["Right Upper Leg In-Out"] = "p30",
            ["Right Upper Leg Twist In-Out"] = "p31",
            ["Right Lower Leg Stretch"] = "p32",
            ["Right Lower Leg Twist In-Out"] = "p33",
            ["Right Foot Up-Down"] = "p34",
            ["Right Foot Twist In-Out"] = "p35",
            ["Right Toes Up-Down"] = "p36",
            ["Left Shoulder Down-Up"] = "p37",
            ["Left Shoulder Front-Back"] = "p38",
            ["Left Arm Down-Up"] = "p39",
            ["Left Arm Front-Back"] = "p40",
            ["Left Arm Twist In-Out"] = "p41",
            ["Left Forearm Stretch"] = "p42",
            ["Left Forearm Twist In-Out"] = "p43",
            ["Left Hand Down-Up"] = "p44",
            ["Left Hand In-Out"] = "p45",
            ["Right Shoulder Down-Up"] = "p46",
            ["Right Shoulder Front-Back"] = "p47",
            ["Right Arm Down-Up"] = "p48",
            ["Right Arm Front-Back"] = "p49",
            ["Right Arm Twist In-Out"] = "p50",
            ["Right Forearm Stretch"] = "p51",
            ["Right Forearm Twist In-Out"] = "p52",
            ["Right Hand Down-Up"] = "p53",
            ["Right Hand In-Out"] = "p54",
            ["LeftHand.Thumb.1 Stretched"] = "p55",
            ["LeftHand.Thumb.Spread"] = "p56",
            ["LeftHand.Thumb.2 Stretched"] = "p57",
            ["LeftHand.Thumb.3 Stretched"] = "p58",
            ["LeftHand.Index.1 Stretched"] = "p59",
            ["LeftHand.Index.Spread"] = "p60",
            ["LeftHand.Index.2 Stretched"] = "p61",
            ["LeftHand.Index.3 Stretched"] = "p62",
            ["LeftHand.Middle.1 Stretched"] = "p63",
            ["LeftHand.Middle.Spread"] = "p64",
            ["LeftHand.Middle.2 Stretched"] = "p65",
            ["LeftHand.Middle.3 Stretched"] = "p66",
            ["LeftHand.Ring.1 Stretched"] = "p67",
            ["LeftHand.Ring.Spread"] = "p68",
            ["LeftHand.Ring.2 Stretched"] = "p69",
            ["LeftHand.Ring.3 Stretched"] = "p70",
            ["LeftHand.Little.1 Stretched"] = "p71",
            ["LeftHand.Little.Spread"] = "p72",
            ["LeftHand.Little.2 Stretched"] = "p73",
            ["LeftHand.Little.3 Stretched"] = "p74",
            ["RightHand.Thumb.1 Stretched"] = "p75",
            ["RightHand.Thumb.Spread"] = "p76",
            ["RightHand.Thumb.2 Stretched"] = "p77",
            ["RightHand.Thumb.3 Stretched"] = "p78",
            ["RightHand.Index.1 Stretched"] = "p79",
            ["RightHand.Index.Spread"] = "p80",
            ["RightHand.Index.2 Stretched"] = "p81",
            ["RightHand.Index.3 Stretched"] = "p82",
            ["RightHand.Middle.1 Stretched"] = "p83",
            ["RightHand.Middle.Spread"] = "p84",
            ["RightHand.Middle.2 Stretched"] = "p85",
            ["RightHand.Middle.3 Stretched"] = "p86",
            ["RightHand.Ring.1 Stretched"] = "p87",
            ["RightHand.Ring.Spread"] = "p88",
            ["RightHand.Ring.2 Stretched"] = "p89",
            ["RightHand.Ring.3 Stretched"] = "p90",
            ["RightHand.Little.1 Stretched"] = "p91",
            ["RightHand.Little.Spread"] = "p92",
            ["RightHand.Little.2 Stretched"] = "p93",
            ["RightHand.Little.3 Stretched"] = "p94",
        };

        /// <summary>
        /// プロパティ名がマッスル番号に対応する場合、"p00"のような名称で代替されるプロパティ名を取得する
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static string GetMuscleIndexPropNameOrAsIs(string propertyName)
            => _propNameRenames.TryGetValue(propertyName, out var name) ? name : propertyName;

        /// <summary>
        /// マッスルを使う/使わないのフラグについて、ケースに応じてフラグをオフにします。
        /// 直近の想定用途として「強制的に上半身のみのモーションを作る」という使い方を想定しています。
        /// </summary>
        /// <param name="flags"></param>
        public static void MaskUsedMuscleFlags(bool[] flags, MuscleFlagMaskStyle maskStyle)
        {
            if (flags == null || flags.Length < 95)
            {
                //マッスルのフラグ一覧ではないものが渡ってきてる→無視
                return;
            }
            
            switch (maskStyle)
            {
                case MuscleFlagMaskStyle.All:
                    return;
                case MuscleFlagMaskStyle.OnlyUpperBody:
                    for (int i = 21; i < 37; i++)
                    {
                        flags[i] = false;
                    }                    
                    return;
                default:
                    return;
            }
        }
    }

    /// <summary> マッスルのフラグをマスクするときの方式一覧 </summary>
    public enum MuscleFlagMaskStyle
    {
        /// <summary> すべて残す = 何もしない </summary>
        All,
        /// <summary> 上半身のみ残す = 下半身のボーンは仮にAnimationCurve情報があっても無視する </summary>
        OnlyUpperBody,
    }
}
