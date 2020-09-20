using Baku.VMagicMirror.ExternalTracker;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 外部トラッキングアプリがXYZオフセットをサポートしている場合に
    /// そのオフセットを体に効かせる処理です。
    /// </summary>
    public class ExternalTrackerBodyOffset : MonoBehaviour
    {
        [Tooltip("受け取った値の適用スケール。割と小さい方がいいかも")]
        [SerializeField] private Vector3 applyScale = new Vector3(0.3f, 0.3f, 0.3f);
        //NOTE: 移動量もフレーバー程度ということで小さめに。
        [SerializeField] private Vector3 applyMin = new Vector3(-0.05f, -0.05f, -0.02f);
        [SerializeField] private Vector3 applyMax = new Vector3(0.05f, 0.05f, 0.02f);

        //NOTE: この場合もxの比率は1.0にはせず、代わりに首回転にもとづく胴体の回転で並進が載るのに頼る
        [SerializeField] private Vector3 applyScaleWhenNoHandTrack = new Vector3(0.8f, 1f, 0.6f);
        [SerializeField] private Vector3 applyMinWhenNoHandTrack = new Vector3(-0.2f, -0.2f, -0.1f);
        [SerializeField] private Vector3 applyMaxWhenNoHandTrack = new Vector3(0.2f, 0.2f, 0.1f);
        
        [SerializeField] private float lerpFactor = 18f;

        private FaceControlConfiguration _config;
        private ExternalTrackerDataSource _externalTracker;
        
        [Inject]
        public void Initialize(FaceControlConfiguration config, ExternalTrackerDataSource externalTracker)
        {
            _config = config;
            _externalTracker = externalTracker;
        }
        
        public Vector3 BodyOffset { get; private set; }
        public bool NoHandTrackMode { get; set; }

        private void Update()
        {
            if (_config.ControlMode != FaceControlModes.ExternalTracker ||
                !_externalTracker.SupportFacePositionOffset)
            {
                BodyOffset = Vector3.zero;
                return;
            }
            
            var offset = _externalTracker.HeadPositionOffset;
            
            var (scale, min, max) = NoHandTrackMode
                ? (applyScaleWhenNoHandTrack, applyMinWhenNoHandTrack, applyMaxWhenNoHandTrack)
                : (applyScale, applyMin, applyMax);
            
            var goal = _externalTracker.Connected
                ? new Vector3(
                    Mathf.Clamp(offset.x * scale.x, min.x, max.x),
                    Mathf.Clamp(offset.y * scale.y, min.z, max.z),
                    Mathf.Clamp(offset.z * scale.z, min.z, max.z)
                )
                : Vector3.zero;

            BodyOffset = Vector3.Lerp(BodyOffset, goal, lerpFactor * Time.deltaTime);
        }
    }
}
