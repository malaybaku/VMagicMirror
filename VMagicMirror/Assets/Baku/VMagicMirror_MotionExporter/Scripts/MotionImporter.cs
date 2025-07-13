using System;
using System.Collections.Generic;
using UnityEngine;

namespace Baku.VMagicMirror.MotionExporter
{
    /// <summary> 文字列化されたモーションデータをランタイムでAnimationClipに戻す処理の実装</summary>
    public class MotionImporter
    {
        /// <summary>
        /// json文字列を指定してモーション情報として再構成します。JSONのデシリアライズに関する例外がスローされる場合があります。
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public SerializedMotion LoadSerializedMotion(string json) 
            => JsonUtility.FromJson<SerializedMotion>(json);

        /// <summary>
        /// デシリアライズした情報を、更に再生可能なデータに変換します。
        /// </summary>
        /// <param name="motion"></param>
        /// <returns></returns>
        public DeserializedMotionClip Deserialize(SerializedMotion motion) 
        {
            var result = new DeserializedMotionClip();
            foreach (var binding in motion.curveBindings)
            {
                result.SetCurve(binding.propertyName, DeserializeToCurve(binding.curve));
            }
            return result;
        }

        private static AnimationCurve DeserializeToCurve(SerializedMotionCurve curve)
        {
            var result = new AnimationCurve
            {
                preWrapMode = WrapMode.Clamp,
                postWrapMode = WrapMode.Clamp,
            };

            var keyframes = DeserializeKeyFrames(curve.b64KeyFrames);
            foreach (var keyFrame in keyframes)
            {
                result.AddKey(DeserializeToKeyFrame(keyFrame));                
            }
            return result;
        }

        private static List<SerializedMotionKeyFrame> DeserializeKeyFrames(string b64keyFrames)
        {
            var result = new List<SerializedMotionKeyFrame>();
            var bytes = Convert.FromBase64String(b64keyFrames);
            //ブロックごとに読み取っていくのでこんなかんじ。
            //NOTE: ここはバージョン差があると読み込みがトラブりやすい場所なので注意！
            for (int i = 0; i + SerializedMotionKeyFrame.BinaryCount <= bytes.Length; i+= SerializedMotionKeyFrame.BinaryCount)
            {
                result.Add(ReadFromBytes(bytes, i));
            }
            return result;
        }

        private static SerializedMotionKeyFrame ReadFromBytes(byte[] bytes, int index)
        {
            return new SerializedMotionKeyFrame()
            {
                time = BitConverter.ToSingle(bytes, index),
                value = BitConverter.ToSingle(bytes, index + 4),
                inTangent = BitConverter.ToSingle(bytes, index + 8),
                inWeight = BitConverter.ToSingle(bytes, index + 12),
                outTangent = BitConverter.ToSingle(bytes, index + 16),
                outWeight = BitConverter.ToSingle(bytes, index + 20),
                weightedMode = BitConverter.ToInt32(bytes, index + 24),
            };
        }

        
        private static Keyframe DeserializeToKeyFrame(SerializedMotionKeyFrame keyFrame)
        {
            return new Keyframe(
                keyFrame.time, 
                keyFrame.value,
                keyFrame.inTangent, 
                keyFrame.outTangent,
                keyFrame.inWeight,
                keyFrame.outWeight
            )
            {
                weightedMode = SerializeKeyframeParams.GetWeighedMode(keyFrame.weightedMode)
            };
        }
    }
}
