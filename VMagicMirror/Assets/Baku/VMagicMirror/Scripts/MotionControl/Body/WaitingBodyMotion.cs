using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>BodyIKの位置を使って呼吸ライクな動きを作るための処理</summary>
    public class WaitingBodyMotion : MonoBehaviour
    {
        [SerializeField] private Vector3 motionSize;
        [SerializeField] private Vector3 durations = Vector3.one;

        public Vector3 MotionSize
        {
            get => motionSize;
            set => motionSize = value;
        }

        public Vector3 Durations
        {
            get => durations;
            set => durations = value;
        }

        public Vector3 Offset { get; private set; }

        private void Update()
        {
            float factor = Time.time * Mathf.PI * 2.0f;

            var p = new Vector3(
                Mathf.Sin(factor / durations.x),
                Mathf.Sin(factor / durations.y),
                Mathf.Sin(factor / durations.z)
            );
            
            Offset = new Vector3(
                motionSize.x * p.x * p.x, 
                motionSize.y * p.y * p.y,
                motionSize.z * p.z * p.z
            );
        }
    }
}
