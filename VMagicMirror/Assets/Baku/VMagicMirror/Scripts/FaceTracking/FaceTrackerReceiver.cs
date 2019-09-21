using System.Linq;
using UnityEngine;
using UniRx;

namespace Baku.VMagicMirror
{
    [RequireComponent(typeof(FaceTracker))]
    public class FaceTrackerReceiver : MonoBehaviour
    {
        [SerializeField]
        private ReceivedMessageHandler handler;

        private FaceTracker _faceTracker;

        private bool _enableFaceTracking = true;
        private string _cameraDeviceName = "";

        private void Start()
        {
            _faceTracker = GetComponent<FaceTracker>();

            handler.Commands.Subscribe(message =>
            {
                switch (message.Command)
                {
                    case MessageCommandNames.SetCameraDeviceName:
                        SetCameraDeviceName(message.Content);
                        break;
                    case MessageCommandNames.EnableFaceTracking:
                        EnableFaceTracking(message.ToBoolean());
                        break;
                    case MessageCommandNames.CalibrateFace:
                        _faceTracker.StartCalibration();
                        break;
                    case MessageCommandNames.SetCalibrateFaceData:
                        _faceTracker.SetCalibrateData(message.Content);
                        break;
                    case MessageCommandNames.FaceDefaultFun:
                        Debug.LogWarning($"{nameof(MessageCommandNames.FaceDefaultFun)}のハンドラがまだないんじゃないかな？？");
                        break;
                }
            });

            handler.QueryRequested += query =>
            {
                if (query.Command == MessageQueryNames.CameraDeviceNames)
                {
                    query.Result = string.Join("\t", GetCameraDeviceNames());
                }
            };
        }

        private void EnableFaceTracking(bool enable)
        {
            if (_enableFaceTracking == enable)
            {
                return;
            }

            _enableFaceTracking = enable;
            UpdateFaceDetectorState();
        }

        private void SetCameraDeviceName(string content)
        {
            _cameraDeviceName = content;
            UpdateFaceDetectorState();
        }
        
        private void UpdateFaceDetectorState()
        {
            if (_enableFaceTracking && !string.IsNullOrWhiteSpace(_cameraDeviceName))
            {
                _faceTracker.ActivateCamera(_cameraDeviceName);
            }
            else
            {
                _faceTracker.StopCamera();
            }
        }

        private string[] GetCameraDeviceNames()
            => WebCamTexture.devices
            .Select(d => d.name)
            .ToArray();
        
    }
}
