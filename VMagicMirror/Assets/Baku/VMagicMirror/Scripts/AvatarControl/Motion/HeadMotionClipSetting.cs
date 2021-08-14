using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 首の動きだけからなるようなモーションの再生の設定
    /// ほぼハードコーディング
    /// </summary>
    [CreateAssetMenu(menuName = "VMagicMirror/HeadMotionClipSetting")]
    public class HeadMotionClipSetting : ScriptableObject
    {
        [Serializable]
        public class HeadMotionClipItem
        {
            [SerializeField] private AnimationCurve angleCurve;
            [Range(0f, 1f)]
            [SerializeField] public float angleRandomFactorMin = 0.5f;
            //角度を小さくしたぶん素早くする、という関係をどこまで厳格に適用するか定めるファクター
            //0寄りにすると、小さめの動きが見かけ上ややゆっくりに見えます。
            [Range(0f, 1f)]
            [SerializeField] public float angleToDurationFactor = 1.0f;

            [SerializeField] public float minDuration = 0.7f;
            [SerializeField] public float maxDuration = 1.0f;

            [Range(0f, 1f)]
            [SerializeField] public float blinkPossibility = 0.8f;
            //モーションの最初～途中のどこかにランダムでまばたきを入れるにあたり、どの位置にするか、という値
            [Range(0f, 1f)]
            [SerializeField] public float blinkTimeRateMax = 0.5f;

            public float Evaluate(float rate) => angleCurve.Evaluate(Mathf.Clamp01(rate));
        }

        [SerializeField] private HeadMotionClipItem[] noddingItems;
        [SerializeField] private HeadMotionClipItem[] shakingItems;
        
        public HeadMotionParams GetNoddingMotionParams()
        {
            var item = noddingItems[Random.Range(0, noddingItems.Length)];
            return new HeadMotionParams(item, false);
        }
        
        public HeadMotionParams GetShakingMotionParams()
        {
            var item = noddingItems[Random.Range(0, shakingItems.Length)];
            //首振りの方向を逆にしてもいい、ということ
            return new HeadMotionParams(item, Random.Range(0f, 1f) < 0.5f);
        }
        
        public readonly struct HeadMotionParams
        {
            public HeadMotionParams(HeadMotionClipItem item, bool reverseCurve)
            {
                _item = item;
                _curveFactor = reverseCurve ? -1f : 1f;

                //Randomを呼ぶ以上はファクトリメソッド使った方がしっくり来るんだけど、まあ細かく気にしない方向で…
                float angleFactor = Random.Range(item.angleRandomFactorMin, 1f);
                float durationFactor = Mathf.Lerp(1.0f, angleFactor, item.angleToDurationFactor);
                float duration = durationFactor * Random.Range(item.minDuration, item.maxDuration);
                bool shouldBlink = Random.Range(0f, 1f) < item.blinkPossibility;
                float blinkTime = shouldBlink ? duration * Random.Range(0f, item.blinkTimeRateMax) : 0f;
                
                Duration = duration;
                AngleFactor = angleFactor;
                ShouldBlink = shouldBlink;
                BlinkTime = blinkTime;
            }

            private readonly HeadMotionClipItem _item;
            private readonly float _curveFactor;
            
            public readonly float Duration;
            public readonly float AngleFactor;
            public readonly bool ShouldBlink;
            public readonly float BlinkTime;

            public float Evaluate(float rate) => _curveFactor * _item.Evaluate(rate);
        }
    }
}

