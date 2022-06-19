using System.Collections;
using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary> 自動まばたき用の値計算と、指示があれば実際に値を適用するやつ </summary>
    public class VRMAutoBlink : MonoBehaviour
    {
        [SerializeField] private float closeDuration = 0.05f;

        [SerializeField] private float closeKeepDuration = 0.04f;

        [SerializeField] private float openDuration = 0.1f;

        [SerializeField] private float minInterval = 3.0f;

        [SerializeField] private float maxInterval = 12.0f;

        //たまに2回連続でまばたきさせるときの、まばたき確率
        [SerializeField, Range(0, 1)] private float doubleBlinkPossibility = 0.2f;

        private readonly SingleValueBlinkSource _blinkSource = new SingleValueBlinkSource();
        public IBlinkSource BlinkSource => _blinkSource;

        private float _intervalCountDown = 0f;
        private bool _isBlinking = false;

        /// <summary>
        /// 強制的にまばたきを開始した状態にします。
        /// 呼び出したフレームか、その次のフレームで瞬き動作が開始されます。
        /// </summary>
        public void ForceStartBlink() => _intervalCountDown = 0;

        private void Start() => _intervalCountDown = Random.Range(minInterval, maxInterval);

        private void Update()
        {
            _intervalCountDown -= Time.deltaTime;
            if (_intervalCountDown > 0)
            {
                return;
            }

            float possibility = Random.Range(0.0f, 1.0f);
            bool doubleBlink = (possibility < doubleBlinkPossibility);

            float totalDuration =
                doubleBlink ?
                (openDuration + closeKeepDuration + closeDuration) * 2 :
                (openDuration + closeKeepDuration + closeDuration);

            _intervalCountDown = Random.Range(minInterval + totalDuration, maxInterval + totalDuration);

            //NOTE: まばたき中にForceStartBlinkが呼ばれたらスルーしたい(二重にコルーチンが走るのを防ぎたい)のでこうしてます
            if (!_isBlinking)
            {
                _isBlinking = true;
                StartCoroutine(BlinkCoroutine(doubleBlink ? 2 : 1));
            }
        }

        private IEnumerator BlinkCoroutine(int blinkCount)
        {
            for (var _ = 0; _ < blinkCount; _++)
            {
                var timeCount = 0f;
                while (timeCount < closeDuration)
                {
                    _blinkSource.Value = ClosingRateCurve(timeCount / closeDuration);
                    timeCount += Time.deltaTime;
                    yield return null;
                }

                while (timeCount < closeDuration + closeKeepDuration)
                {
                    var keepCount = timeCount - closeDuration;
                    _blinkSource.Value = 1f;
                    timeCount += Time.deltaTime;
                    yield return null;
                }

                while (timeCount < closeDuration + closeKeepDuration + openDuration)
                {
                    var openCount = timeCount - closeDuration - closeKeepDuration;
                    _blinkSource.Value = OpeningRateCurve(openCount / openDuration);
                    timeCount += Time.deltaTime;
                    yield return null;
                }
            }

            //途中の動作がカクカクになった場合も最後は目を開いた状態で辻褄をあわせる
            _blinkSource.Value = 0;
            _isBlinking = false;
        }

        private static float ClosingRateCurve(float rate) => Mathf.Clamp01(rate);
        
        private static float OpeningRateCurve(float rate) => Mathf.Clamp01(1.0f - rate);

        /// <summary> 左目と右目が同じ動き方で瞬きするまばたき値 </summary>
        class SingleValueBlinkSource : IBlinkSource
        {
            public float Value { get; set; }
            public float Left => Value;
            public float Right => Value;
        }
    }
}
