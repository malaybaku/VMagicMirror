using System;
using UnityEngine;

namespace mattatz.TransformControl
{
    public static class TransformControlCameraStore
    {
        private static Camera _camera;
        public static Camera Get()
        {
            if (_camera == null)
            {
                throw new InvalidOperationException(
                    "TransformControlCameraStore is not initialized. CameraUtilWrapper.Initialize() must set RefCameraForRay before TransformControl is used."
                );
            }

            return _camera;
        }

        public static void Set(Camera camera)
        {
            if (camera == null)
            {
                throw new ArgumentNullException(nameof(camera));
            }

            _camera = camera;
        }
    }
}

