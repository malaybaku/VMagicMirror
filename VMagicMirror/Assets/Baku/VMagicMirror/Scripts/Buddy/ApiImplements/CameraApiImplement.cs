using UnityEngine;

namespace Baku.VMagicMirror.Buddy
{
    public class CameraApiImplement
    {
        private readonly Camera _mainCamera;
        private readonly Transform _transform;
        public CameraApiImplement(Camera mainCamera)
        {
            _mainCamera = mainCamera;
            _transform = mainCamera.transform;
        }

        public Vector3 GetPosition() => _transform.position;
        public Quaternion GetRotation() => _transform.rotation;
        public float GetFieldOfView() => _mainCamera.fieldOfView;
    }
}
