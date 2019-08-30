using System.Collections.Generic;
using UnityEngine;
using VRM;

namespace Baku.VMagicMirror
{
    public class BuiltInMotionRequest
    {
        public BuiltInMotionRequest(string word, AnimationClip animation, Dictionary<BlendShapeKey, float> blendShape)
        {
            Word = word;
            Duration = animation.length;
            Animation = animation;
            BlendShape = blendShape ?? new Dictionary<BlendShapeKey, float>();
        }

        public BuiltInMotionRequest(string word, float duration, Dictionary<BlendShapeKey, float> blendShape)
        {
            Word = word;
            Duration = duration;
            Animation = null;
            BlendShape = blendShape ?? new Dictionary<BlendShapeKey, float>();
        }

        /// <summary>
        /// このリクエストに対応するワードを文字列で取得します。
        /// </summary>
        public string Word { get; }

        /// <summary>
        /// アニメーションがnullの場合、表情を維持すべき時間を[sec]単位で取得します。
        /// </summary>
        /// <remarks>
        /// アニメーションが非nullの場合、<see cref="Animation.length"/>と同じ値で初期化
        /// </remarks>
        public float Duration { get; }

        /// <summary>
        /// メインの動作を指定するアニメーション。TODO: ここを唐突にBVHのbyte[]とかにするかも。要注意。
        /// </summary>
        public AnimationClip Animation { get; }

        /// <summary>
        /// 動作中の表情を指定するブレンドシェイプ。表情を変える必要が無い場合は空ディクショナリ。
        /// </summary>
        public IReadOnlyDictionary<BlendShapeKey, float> BlendShape { get; }

    }
}
