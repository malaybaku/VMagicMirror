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

        private void Update()
        {
            if (_config.ControlMode != FaceControlModes.ExternalTracker ||
                !_externalTracker.SupportFacePositionOffset)
            {
                BodyOffset = Vector3.zero;
                return;
            }
            
            var offset = _externalTracker.HeadPositionOffset;

            var goal = _externalTracker.Connected
                ? new Vector3(
                    Mathf.Clamp(offset.x * applyScale.x, applyMin.x, applyMax.x),
                    Mathf.Clamp(offset.y * applyScale.y, applyMin.z, applyMax.z),
                    Mathf.Clamp(offset.z * applyScale.z, applyMin.z, applyMax.z)
                )
                : Vector3.zero;

            BodyOffset = Vector3.Lerp(BodyOffset, goal, lerpFactor * Time.deltaTime);
        }
    }
}
