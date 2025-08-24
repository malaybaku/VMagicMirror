using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Profiling;

namespace Baku.VMagicMirror.ExternalTracker.iFacialMocap
{
    public static class iFacialMocapDataParseUtil
    {
        private static readonly string[] BlendShapeNames = new[]
        {
            //目
            iFacialMocapBlendShapeNames.eyeBlinkLeft,
            iFacialMocapBlendShapeNames.eyeLookUpLeft,
            iFacialMocapBlendShapeNames.eyeLookDownLeft,
            iFacialMocapBlendShapeNames.eyeLookInLeft,
            iFacialMocapBlendShapeNames.eyeLookOutLeft,
            iFacialMocapBlendShapeNames.eyeWideLeft,
            iFacialMocapBlendShapeNames.eyeSquintLeft,

            iFacialMocapBlendShapeNames.eyeBlinkRight,
            iFacialMocapBlendShapeNames.eyeLookUpRight,
            iFacialMocapBlendShapeNames.eyeLookDownRight,
            iFacialMocapBlendShapeNames.eyeLookInRight,
            iFacialMocapBlendShapeNames.eyeLookOutRight,
            iFacialMocapBlendShapeNames.eyeWideRight,
            iFacialMocapBlendShapeNames.eyeSquintRight,

            //あご
            iFacialMocapBlendShapeNames.jawOpen,
            iFacialMocapBlendShapeNames.jawForward,
            iFacialMocapBlendShapeNames.jawLeft,
            iFacialMocapBlendShapeNames.jawRight,

            //まゆげ
            iFacialMocapBlendShapeNames.browDownLeft,
            iFacialMocapBlendShapeNames.browOuterUpLeft,
            iFacialMocapBlendShapeNames.browDownRight,
            iFacialMocapBlendShapeNames.browOuterUpRight,
            iFacialMocapBlendShapeNames.browInnerUp,

            //口(多い)
            iFacialMocapBlendShapeNames.mouthLeft,
            iFacialMocapBlendShapeNames.mouthSmileLeft,
            iFacialMocapBlendShapeNames.mouthFrownLeft,
            iFacialMocapBlendShapeNames.mouthPressLeft,
            iFacialMocapBlendShapeNames.mouthUpperUpLeft,
            iFacialMocapBlendShapeNames.mouthLowerDownLeft,
            iFacialMocapBlendShapeNames.mouthStretchLeft,
            iFacialMocapBlendShapeNames.mouthDimpleLeft,

            iFacialMocapBlendShapeNames.mouthRight,
            iFacialMocapBlendShapeNames.mouthSmileRight,
            iFacialMocapBlendShapeNames.mouthFrownRight,
            iFacialMocapBlendShapeNames.mouthPressRight,
            iFacialMocapBlendShapeNames.mouthUpperUpRight,
            iFacialMocapBlendShapeNames.mouthLowerDownRight,
            iFacialMocapBlendShapeNames.mouthStretchRight,
            iFacialMocapBlendShapeNames.mouthDimpleRight,

            iFacialMocapBlendShapeNames.mouthClose,
            iFacialMocapBlendShapeNames.mouthFunnel,
            iFacialMocapBlendShapeNames.mouthPucker,
            iFacialMocapBlendShapeNames.mouthShrugUpper,
            iFacialMocapBlendShapeNames.mouthShrugLower,
            iFacialMocapBlendShapeNames.mouthRollUpper,
            iFacialMocapBlendShapeNames.mouthRollLower,

            //鼻
            iFacialMocapBlendShapeNames.noseSneerLeft,
            iFacialMocapBlendShapeNames.noseSneerRight,

            //ほお
            iFacialMocapBlendShapeNames.cheekPuff,
            iFacialMocapBlendShapeNames.cheekSquintLeft,
            iFacialMocapBlendShapeNames.cheekSquintRight,

            //舌
            iFacialMocapBlendShapeNames.tongueOut,
        };

        private static readonly Dictionary<int, string[]> LengthAndNames = new();

        static iFacialMocapDataParseUtil()
        {
            var listBasedDict = new Dictionary<int, List<string>>();
            foreach (var key in BlendShapeNames)
            {
                if (!listBasedDict.ContainsKey(key.Length))
                {
                    listBasedDict[key.Length] = new List<string>();
                }
                listBasedDict[key.Length].Add(key);
            }

            foreach (var kv in listBasedDict)
            {
                LengthAndNames[kv.Key] = kv.Value.ToArray();
            }
        }

        public static bool TryGetBlendShapeName(ReadOnlySpan<char> span, out string result)
        {
            if (!LengthAndNames.TryGetValue(span.Length, out var keys))
            {
                result = null;
                return false;
            }

            foreach (var key in keys)
            {
                if (span.SequenceEqual(key.AsSpan()))
                {
                    result = key;
                    return true;
                }
            }

            result = null;
            return false;
        }

        /// <summary>
        /// カンマ区切りのfloat値を、maxCountで指定した数を最大値として読み取る
        /// </summary>
        /// <param name="span"></param>
        /// <param name="result"></param>
        /// <param name="maxCount"></param>
        public static void ParseCommaSeparatedFloats(ReadOnlySpan<char> span, List<float> result, int maxCount)
        {
            result.Clear();
            while (result.Count < maxCount)
            {
                var commaIndex = span.IndexOf(',');
                var isLast = commaIndex < 0;
                // commaがない場合、末尾の有効な値を読んでる可能性があることに注意
                var part = isLast ? span : span[..commaIndex];
                if (ParseUtil.FloatParse(part, out var value))
                {
                    result.Add(value);
                }
                else
                {
                    // 一つでもパースできなければ中断(普通は通過しないはず)
                    return;
                }

                if (!isLast)
                {
                    span = span[(commaIndex + 1)..];
                }
            }
        }

        public static int FindEqualCharIndex(StringBuilder src)
        {
            var findCount = 0;
            var result = -1;
            for (var i = 0; i < src.Length; i++)
            {
                if (src[i] == '=')
                {
                    if (findCount == 0)
                    {
                        findCount = 1;
                        result = i;
                    }
                    else
                    {
                        return -1;
                    }
                }
            }
            return result;
        }
    }
}