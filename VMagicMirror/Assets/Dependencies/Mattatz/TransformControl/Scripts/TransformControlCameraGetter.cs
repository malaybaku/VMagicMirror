using UnityEngine;

namespace mattatz.TransformControl
{
    public static class TransformControlCameraStore
    {
        private static Camera _camera;
        public static Camera Get() => _camera != null ? _camera : Camera.main;
        public static void Set(Camera camera) => _camera = camera;
    }
}

