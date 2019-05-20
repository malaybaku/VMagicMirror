using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRM;

namespace Baku.VMagicMirror
{
    [RequireComponent(typeof(VRMBlendShapeProxy))]
    public class VRMBlink : MonoBehaviour
    {

        [SerializeField]
        AnimationCurve closeCurve = new AnimationCurve(new[]
        {
            new Keyframe(0, 0),
            new Keyframe(1, 1),
        });

        [SerializeField]
        private float closeDuration = 0.1f;

        [SerializeField]
        AnimationCurve openCurve = new AnimationCurve(new[]
        {
            new Keyframe(0, 1),
            new Keyframe(1, 0),
        });

        [SerializeField]
        private float openDuration = 0.1f;

        [SerializeField]
        private float minInterval = 3.0f;

        [SerializeField]
        private float maxInterval = 10.0f;

        [SerializeField, Range(0, 1)]
        private float doubleBlinkPossibility = 0.2f;

        private VRMBlendShapeProxy blendShapeProxy;

        private float intervalCountDown = 0f;
        //private float blinkCountUp = 0f;

        void Start()
        {
            intervalCountDown = Random.Range(minInterval, maxInterval);
            blendShapeProxy = GetComponent<VRMBlendShapeProxy>();
        }

        void Update()
        {
            intervalCountDown -= Time.deltaTime;
            if (intervalCountDown > 0)
            {
                return;
            }

            float possibility = Random.Range(0.0f, 1.0f);
            bool doubleBlink = (possibility < doubleBlinkPossibility);

            float totalDuration =
                doubleBlink ?
                (openDuration + closeDuration) * 2 :
                (openDuration + closeDuration);

            intervalCountDown = Random.Range(minInterval + totalDuration, maxInterval + totalDuration);

            var blinks = new List<BlinkCondition>()
            {
                new BlinkCondition { curve = closeCurve, duration = closeDuration },
                new BlinkCondition { curve = openCurve, duration = openDuration },
            };
            if (doubleBlink)
            {
                blinks.Add(new BlinkCondition { curve = closeCurve, duration = closeDuration });
                blinks.Add(new BlinkCondition { curve = openCurve, duration = openDuration });
            }

            StartCoroutine(BlinkCoroutine(blinks));
        }

        private IEnumerator BlinkCoroutine(IEnumerable<BlinkCondition> blinks)
        {
            foreach (var blink in blinks)
            {
                for (float count = 0; count < blink.duration;)
                {
                    count += Time.deltaTime;
                    float blendRate = blink.curve.Evaluate(count / blink.duration);
                    blendShapeProxy.ImmediatelySetValue(BlendShapePreset.Blink_L, blendRate);
                    blendShapeProxy.ImmediatelySetValue(BlendShapePreset.Blink_R, blendRate);
                    yield return null;
                }
            }
        }

        private struct BlinkCondition
        {
            public AnimationCurve curve;
            public float duration;
        }
    }
}

