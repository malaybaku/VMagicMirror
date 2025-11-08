using Baku.VMagicMirror.MediaPipeTracker;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary> 頭のFK処理のアクティブ状態を制御するやつ </summary>
    public class HeadFKIntegrator : MonoBehaviour
    {
        [SerializeField] private FaceAttitudeController imageAttitude = null;
        [SerializeField] private ExternalTrackerFaceAttitudeController externalTrackerAttitude = null;
        [SerializeField] private MediaPipeFaceAttitudeController mediaPipeFaceAttitude = null;
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
            switch (_config.HeadMotionControlModeValue)
            {
            case FaceControlModes.ExternalTracker:
                externalTrackerAttitude.IsActive = true;
                mediaPipeFaceAttitude.IsActive = false;
                imageAttitude.IsActive = false;
                break;
            case FaceControlModes.WebCamLowPower:
            case FaceControlModes.WebCamHighPower:
                externalTrackerAttitude.IsActive = false;
                mediaPipeFaceAttitude.IsActive = true;
                imageAttitude.IsActive = false;
                break;
            default:
                externalTrackerAttitude.IsActive = false;
                mediaPipeFaceAttitude.IsActive = false;
                imageAttitude.IsActive = false;
                break;
            }

            nonImageBasedMotion.FaceControlMode = _config.HeadMotionControlModeValue;
        }
    }
}
