using UnityEngine;
using UniRx;

namespace Baku.VMagicMirror
{
    [RequireComponent(typeof(Camera))]
    [RequireComponent(typeof(CameraTransformController))]
    public class CameraController : MonoBehaviour
    {
        [SerializeField]
        private ReceivedMessageHandler handler = null;

        private Camera _cam = null;
        private CameraTransformController _transformController = null;

        private bool _isInFreeCameraMode = false;
        private bool _customCameraPositionEnabled = false;

        private Vector3 _defaultCameraPosition = Vector3.zero;
        private Vector3 _defaultCameraRotationEuler = Vector3.zero;

        private Vector3 _customCameraPosition = Vector3.zero;
        private Vector3 _customCameraRotationEuler = Vector3.zero;


        void Start()
        {
            _cam = GetComponent<Camera>();
            _defaultCameraPosition = _cam.transform.position;
            _defaultCameraRotationEuler = _cam.transform.rotation.eulerAngles;

            _transformController = GetComponent<CameraTransformController>();

            handler.Commands.Subscribe(message =>
            {
                switch(message.Command)
                {
                    case MessageCommandNames.Chromakey:
                        var argb = message.ToColorFloats();
                        SetCameraBackgroundColor(argb[0], argb[1], argb[2], argb[3]);
                        break;
                    case MessageCommandNames.CameraHeight:
                        SetCameraHeight(message.ParseAsCentimeter());
                        break;
                    case MessageCommandNames.CameraDistance:
                        SetCameraDistance(message.ParseAsCentimeter());
                        break;
                    case MessageCommandNames.CameraVerticalAngle:
                        SetCameraVerticalAngle(message.ToInt());
                        break;
                    case MessageCommandNames.EnableFreeCameraMode:
                        EnableFreeCameraMode(message.ToBoolean());
                        break;
                    case MessageCommandNames.EnableCustomCameraPosition:
                        EnableCustomCameraPosition(message.ToBoolean());
                        break;
                    case MessageCommandNames.SetCustomCameraPosition:
                        SetCustomCameraPosition(message.ToFloatArray());
                        break;
                    default:
                        break;
                }
            });
            handler.QueryRequested += (_, e) =>
            {
                switch (e.Query.Command)
                {
                    case MessageQueryNames.CurrentCameraPosition:
                        var angles = _cam.transform.rotation.eulerAngles;
                        e.Query.Result = string.Join(",", new float[]
                        {
                            _cam.transform.position.x,
                            _cam.transform.position.y,
                            _cam.transform.position.z,
                            angles.x,
                            angles.y,
                            angles.z,
                        });
                        break;
                    default:
                        break;
                }
            };
        }

        private void SetCameraBackgroundColor(float a, float r, float g, float b)
        {
            _cam.backgroundColor = new Color(r, g, b, a);
        }

        private void SetCameraHeight(float height)
        {
            _defaultCameraPosition = new Vector3(
                _defaultCameraPosition.x,
                height,
                _defaultCameraPosition.z
                );
            UpdateCameraTransform();
        }

        private void SetCameraDistance(float distance)
        {
            _defaultCameraPosition = new Vector3(
                _defaultCameraPosition.x,
                _defaultCameraPosition.y,
                distance
                );
            UpdateCameraTransform();
        }

        private void SetCameraVerticalAngle(int angleDegree)
        {
            _defaultCameraRotationEuler = new Vector3(angleDegree, 180, 0);
            UpdateCameraTransform();
        }

        private void EnableCustomCameraPosition(bool v)
        {
            _customCameraPositionEnabled = v;
            UpdateCameraTransform();
        }

        private void EnableFreeCameraMode(bool v)
        {
            _isInFreeCameraMode = v;
            _transformController.enabled = v;
        }

        private void SetCustomCameraPosition(float[] values)
        {
            if (values.Length >= 6)
            {
                _customCameraPosition = new Vector3(
                    values[0], values[1], values[2]
                    );

                _customCameraRotationEuler = new Vector3(
                    values[3], values[4], values[5]
                    );

                UpdateCameraTransform();
            }
        }

        private void UpdateCameraTransform()
        {
            if (_isInFreeCameraMode)
            {
                return;
            }

            if (_customCameraPositionEnabled)
            {
                _cam.transform.position = _customCameraPosition;
                _cam.transform.rotation = Quaternion.Euler(_customCameraRotationEuler);
            }
            else
            {
                _cam.transform.position = _defaultCameraPosition;
                _cam.transform.rotation = Quaternion.Euler(_defaultCameraRotationEuler);
            }
        }
    }
}

