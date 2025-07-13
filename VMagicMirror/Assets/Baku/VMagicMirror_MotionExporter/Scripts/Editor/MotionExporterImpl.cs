using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace Baku.VMagicMirror.MotionExporter
{
    public static class MotionExporterImpl
    {
        public static SerializedMotion GetSerializedMotion(AnimationClip clip)
        {
            if (clip == null)
            {
                Debug.LogError("(EN) Export Target is null. Please specify target animation clip.");
                Debug.LogError("(JP) Export Targetにエクスポートするアニメーションを指定して下さい。");
                return null;
            }
            
            if (!clip.humanMotion)
            {
                Debug.LogError("(EN) Please check Humanoid Animation flag");
                Debug.LogError("(JP) アニメーションがHumanoid用ではありません。アニメーションの設定を確認して下さい。");
                return null;
            }

            return ToSerializedMotion(clip);
        }
        
        private static SerializedMotion ToSerializedMotion(AnimationClip clip)
        {
            var result = new SerializedMotion()
            {
                version = MotionExporterConsts.Version,
            };
            result.curveBindings = new List<SerializedMotionCurveBinding>();

            var bindings = AnimationUtility.GetCurveBindings(clip);
            foreach (var b in bindings.Where(v => v.type == typeof(Animator)))
            {
                var item = new SerializedMotionCurveBinding()
                {
                    path = b.path,
                    propertyName = SerializeMuscleNameMapper.GetMuscleIndexPropNameOrAsIs(b.propertyName),
                    typeName = b.type.Name,
                    isDiscreteCurve = b.isDiscreteCurve,
                    isPPtrCurve = b.isPPtrCurve,
                    curve = SerializeCurve(AnimationUtility.GetEditorCurve(clip, b)),
                };
                result.curveBindings.Add(item);                
            }
            return result;
        }

        private static SerializedMotionCurve SerializeCurve(AnimationCurve curve)
        {
            //NOTE: wrapModeをあえて無視しておく。ループとか現行実装だと手に負えない気がするので…
            var keyFramesBytes = new byte[SerializedMotionKeyFrame.BinaryCount * curve.keys.Length];

            using (var stream = new MemoryStream(keyFramesBytes, true))
            using (var writer = new BinaryWriter(stream))
            {
                for (int i = 0; i < curve.keys.Length; i++)
                {
                    WriteKeyFrameBytes(curve.keys[i], writer);
                }
            }

            return new SerializedMotionCurve()
            {
                b64KeyFrames = Convert.ToBase64String(keyFramesBytes),
            };
        }

        private static void WriteKeyFrameBytes(Keyframe keyframe, BinaryWriter writer)
        {
            var item = SerializeKeyFrame(keyframe);
            writer.Write(item.time);
            writer.Write(item.value);
            writer.Write(item.inTangent);
            writer.Write(item.inWeight);
            writer.Write(item.outTangent);
            writer.Write(item.outWeight);
            writer.Write(item.weightedMode);
        }

        
        private static SerializedMotionKeyFrame SerializeKeyFrame(Keyframe keyframe)
        {
            return new SerializedMotionKeyFrame()
            {
                time = keyframe.time,
                value = keyframe.value,
                inTangent = keyframe.inTangent,
                inWeight = keyframe.inWeight,
                outTangent = keyframe.outTangent,
                outWeight = keyframe.outWeight,
                weightedMode = SerializeKeyframeParams.GetInt(keyframe.weightedMode),
            };
        }
    }
}
