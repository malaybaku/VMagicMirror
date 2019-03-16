using UnityEngine;

namespace Baku.VMagicMirror
{
    public class BodyTargetMove : MonoBehaviour
    {
        public Vector3 motionSize;
        public Vector3 durations = Vector3.one;

        void Update()
        {
            float factor = Time.time * Mathf.PI * 2.0f;

            var p = new Vector3(
                Mathf.Sin(factor / durations.x),
                Mathf.Sin(factor / durations.y),
                Mathf.Sin(factor / durations.z)
                );

            transform.localPosition = new Vector3(
                motionSize.x * p.x * p.x, 
                motionSize.y * p.y * p.y,
                motionSize.z * p.z * p.z
                );
        }
    }
}

