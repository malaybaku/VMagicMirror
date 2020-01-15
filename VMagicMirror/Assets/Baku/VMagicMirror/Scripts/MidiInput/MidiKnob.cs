using UnityEngine;

namespace Baku.VMagicMirror
{
    public class MidiKnob : MonoBehaviour
    {
        private const float ValueScale = 0.08f;
        private float _value = 0f;

        private Transform tiltRoot = null;
        
        private void Awake()
        {
            tiltRoot = transform.GetChild(0);
        }

        /// <summary>
        /// 値を指定するとノブの値に相当する表示をしてくれるすごいやつだよ
        /// </summary>
        /// <param name="value"></param>
        public void SetValue(float value)
        {
            _value = value;
            var scale = tiltRoot.localScale;
            tiltRoot.localScale = new Vector3(
                scale.x,
                value,
                scale.z
                );
        }

        public MidiKnobTargetData GetKnobTargetData()
        {
            var t = transform;
            return new MidiKnobTargetData()
            {
                knobTransform = t,
                position = t.position + tiltRoot.up * (tiltRoot.localScale.y * _value * ValueScale),
            };
        }
    }
}
