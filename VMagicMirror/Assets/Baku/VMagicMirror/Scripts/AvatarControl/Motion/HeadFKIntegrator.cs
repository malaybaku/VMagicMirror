using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary> 頭のFK処理のアクティブ状態を制御するやつ </summary>
    public class HeadFKIntegrator : MonoBehaviour
    {
        [SerializeField] private FaceAttitudeController imageAttitude = null;
        [SerializeField] private ExternalTrackerFaceAttitudeController externalTrackerAttitude = null;
        [SerializeField] private NonImageBasedMotion nonImageBasedMotion = null;

        private FaceControlConfiguration _config;
        
        [Inject]
        public void Initialize(FaceControlConfiguration config)
        {
            _config = config;
        }
        
        private void Update()
        {
            //NOTE: FKはLateUpdateのタイミングで適用されるので、その前のUpdate時点で仕込む、みたいな制御。
            switch (_config.ControlMode)
            {
            case FaceControlModes.ExternalTracker:
                externalTrackerAttitude.IsActive = true;
                imageAttitude.IsActive = false;
                nonImageBasedMotion.IsNoTrackingApplied = false;
                break;
            case FaceControlModes.WebCam:
                externalTrackerAttitude.IsActive = false;
                imageAttitude.IsActive = true;
                nonImageBasedMotion.IsNoTrackingApplied = false;
                break;
            default:
                externalTrackerAttitude.IsActive = false;
                imageAttitude.IsActive = false;
                nonImageBasedMotion.IsNoTrackingApplied = true;
                break;
            }
        }
    }
}
