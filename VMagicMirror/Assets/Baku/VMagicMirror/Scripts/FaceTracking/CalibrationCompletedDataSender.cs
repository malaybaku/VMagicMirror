using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    [RequireComponent(typeof(FaceTracker))]
    public class CalibrationCompletedDataSender : MonoBehaviour
    {
        [Inject] private IMessageSender sender = null;

        private void Start()
        {
            GetComponent<FaceTracker>().CalibrationCompleted += 
                data => sender.SendCommand(MessageFactory.Instance.SetCalibrateFaceData(data));
        }
    }
}

