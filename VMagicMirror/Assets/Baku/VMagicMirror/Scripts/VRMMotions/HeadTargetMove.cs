using UnityEngine;

namespace Baku.VMagicMirror
{
    public class HeadTargetMove : MonoBehaviour
    {
        public Vector3 motionSize;
        //durations MUST be positive value
        public Vector3 durations = Vector3.one;

        void Update()
        {
            float factor = Time.time * Mathf.PI * 2.0f;

            transform.localPosition = new Vector3(
                motionSize.x * Mathf.Sin(factor / durations.x),
                motionSize.y * Mathf.Sin(factor / durations.y),
                motionSize.z * Mathf.Sin(factor / durations.z)
                );
        }
    }
}
