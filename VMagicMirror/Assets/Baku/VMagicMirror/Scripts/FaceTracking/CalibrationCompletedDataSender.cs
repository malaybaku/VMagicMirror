using UnityEngine;

namespace Baku.VMagicMirror
{
    [RequireComponent(typeof(FaceTracker))]
    public class CalibrationCompletedDataSender : MonoBehaviour
    {
        [SerializeField]
        GrpcSender messageSender = null;
        void Start()
        {
            GetComponent<FaceTracker>().CalibrationCompleted += 
                data => messageSender.SendCommand(MessageFactory.Instance.SetCalibrateFaceData(data));
        }
    }
}

