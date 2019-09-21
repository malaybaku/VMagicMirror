﻿using System.Collections;
using UnityEngine;
using VRM;

namespace Baku.VMagicMirror
{
    /// <summary> 自動まばたき用の値計算と、指示があれば実際に値を適用するやつ </summary>
    public class VRMAutoBlink : MonoBehaviour
    {
        private static readonly BlendShapeKey BlinkLKey = new BlendShapeKey(BlendShapePreset.Blink_L);
        private static readonly BlendShapeKey BlinkRKey = new BlendShapeKey(BlendShapePreset.Blink_R);

        [SerializeField]
        private float closeDuration = 0.1f;

        [SerializeField]
        private float openDuration = 0.1f;

        [SerializeField]
        private float minInterval = 3.0f;

        [SerializeField]
        private float maxInterval = 12.0f;

        //たまに2回連続でまばたきさせるときの、まばたき確率
        [SerializeField, Range(0, 1)]
        private float doubleBlinkPossibility = 0.2f;

        private float _intervalCountDown = 0f;
        private float _currentBlinkValue = 0f;

        /// <summary>
        /// 指定されたブレンドシェイプに、現在計算したまばたきブレンドシェイプの値を適用する
        /// </summary>
        /// <param name="proxy"></param>
        public void Apply(VRMBlendShapeProxy proxy)
        {
            proxy.AccumulateValue(BlinkLKey, _currentBlinkValue);
            proxy.AccumulateValue(BlinkRKey, _currentBlinkValue);
        }

        public void Reset(VRMBlendShapeProxy proxy)
        {
            proxy.AccumulateValue(BlinkLKey, 0);
            proxy.AccumulateValue(BlinkRKey, 0);
        }
        
        private void Start()
        {
            _intervalCountDown = Random.Range(minInterval, maxInterval);
        }

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
                (openDuration + closeDuration) * 2 :
                (openDuration + closeDuration);

            _intervalCountDown = Random.Range(minInterval + totalDuration, maxInterval + totalDuration);

            StartCoroutine(BlinkCoroutine(doubleBlink ? 2 : 1));
        }

        private IEnumerator BlinkCoroutine(int blinkCount)
        {
            for (int _ = 0; _ < blinkCount; _++)
            {
                for (float count = 0; count < closeDuration; count += Time.deltaTime)
                {
                    _currentBlinkValue = ClosingRateCurve(count / closeDuration);
                    yield return null;
                }
                
                for (float count = 0; count < openDuration; count += Time.deltaTime)
                {
                    _currentBlinkValue = OpeningRateCurve(count / closeDuration);
                    yield return null;
                }
            }
        }

        private static float ClosingRateCurve(float rate) => Mathf.Clamp01(rate);
        
        private static float OpeningRateCurve(float rate) => Mathf.Clamp01(1.0f - rate);
    }
}
