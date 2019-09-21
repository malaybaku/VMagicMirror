using UnityEngine;

namespace Baku.VMagicMirror
{
    [RequireComponent(typeof(FaceDetector))]
    public class CalibrationCompletedDataSender : MonoBehaviour
    {
        [SerializeField]
        GrpcSender messageSender = null;
        void Start()
        {
            GetComponent<FaceDetector>().CalibrationCompleted += 
                (_, e) => messageSender?.SendCommand(MessageFactory.Instance.SetCalibrateFaceData(e.Data));
        }
    }
}

