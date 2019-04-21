using UnityEngine;
using UniRx;

namespace Baku.VMagicMirror
{
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        [SerializeField]
        private ReceivedMessageHandler handler = null;

        private Camera _cam = null;

        void Start()
        {
            _cam = GetComponent<Camera>();
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
                    default:
                        break;
                }
            });
        }

        private void SetCameraBackgroundColor(float a, float r, float g, float b)
        {
            _cam.backgroundColor = new Color(r, g, b, a);
        }

        private void SetCameraHeight(float height)
        {
            _cam.transform.position = new Vector3(
              _cam.transform.position.x,
              height,
              _cam.transform.position.z
              );
        }

        private void SetCameraDistance(float distance)
        {
            _cam.transform.position = new Vector3(
                 _cam.transform.position.x,
                 _cam.transform.position.y,
                 distance
                 );
        }

        private void SetCameraVerticalAngle(int angleDegree)
        {
            _cam.transform.rotation = Quaternion.Euler(angleDegree, 180, 0);
        }

    }
}

