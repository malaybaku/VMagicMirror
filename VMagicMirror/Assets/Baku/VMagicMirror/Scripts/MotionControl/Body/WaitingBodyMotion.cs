using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>BodyIKの位置を使って呼吸ライクな動きを作るための処理</summary>
    public class WaitingBodyMotion : MonoBehaviour
    {
        [SerializeField] private Vector3 motionSize;
        [SerializeField] private float duration = 10f;

        private float _factor = 0f;
        
        public Vector3 MotionSize
        {
            get => motionSize;
            set => motionSize = value;
        }

        public float Duration
        {
            get => duration;
            set => duration = value;
        }

        /// <summary>
        /// 呼吸に相当する体の動きのオフセットを取得します。
        /// </summary>
        public Vector3 Offset { get; private set; }
        
        /// <summary>
        /// 体の動きの周期を0～1の範囲で取得します。
        /// 0～0.5は息を吸っていくフェーズ、0.5～1.0は息を吐くフェーズに相当します。
        /// </summary>
        public float Phase { get; private set; }
        

        private void Update()
        {
            _factor += Time.deltaTime * Mathf.PI * 2.0f;
            //NOTE:
            //_factorは歴史的経緯によってsin*sinの形で使われているので、
            //位相上は0~PIまでで1周期になる(PI~2PIは0~PIと同じになる)
            _factor = Mathf.Repeat(_factor, duration * Mathf.PI);
            
            Phase = _factor / duration / Mathf.PI;
            
            //float factor = Time.time * Mathf.PI * 2.0f;
            float p = Mathf.Sin(_factor / duration);

            Offset = new Vector3(
                motionSize.x * p * p,
                motionSize.y * p * p,
                motionSize.z * p * p
            );
        }
    }
}
