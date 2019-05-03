using System.Linq;
using UnityEngine;
using UniRx;
using System;

namespace Baku.VMagicMirror
{

    [RequireComponent(typeof(FaceDetector))]
    [RequireComponent(typeof(FaceBlendShapeController))]
    public class FaceDetectionReceiver : MonoBehaviour
    {
        [SerializeField]
        private ReceivedMessageHandler handler;

        private FaceDetector _faceDetector = null;
        private FaceBlendShapeController _blendShapeController = null;


        private bool _enableFaceTracking = true;
        private string _cameraDeviceName = "";

        void Start()
        {
            _faceDetector = GetComponent<FaceDetector>();
            _blendShapeController = GetComponent<FaceBlendShapeController>();

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
                        CalibrateFace();
                        break;
                    case MessageCommandNames.SetCalibrateFaceData:
                        SetCalibrateFaceData(message.Content);
                        break;
                    case MessageCommandNames.FaceDefaultFun:
                        SetFaceDefaultFunValue(message.ParseAsPercentage());
                        break;
                    default:
                        break;
                }
            });

            handler.QueryRequested += (_, e) =>
            {
                switch (e.Query.Command)
                {
                    case MessageQueryNames.CameraDeviceNames:
                        e.Query.Result = string.Join("\t", GetCameraDeviceNames());
                        break;
                    default:
                        break;
                }
            };
        }

        private void SetCalibrateFaceData(string content)
        {
            _faceDetector.SetCalibrateData(content);
        }

        private void CalibrateFace()
        {
            _faceDetector.StartCalibration();
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
                _faceDetector.ActivateCamera(_cameraDeviceName);
            }
            else
            {
                _faceDetector.StopCamera();
            }
        }

        private string[] GetCameraDeviceNames()
            => WebCamTexture.devices
            .Select(d => d.name)
            .ToArray();

        private void SetFaceDefaultFunValue(float v)
        {
            _blendShapeController.FaceDefaultFunValue = v;
        }


    }
}
