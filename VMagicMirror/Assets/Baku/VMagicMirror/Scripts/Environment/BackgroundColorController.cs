using UnityEngine;

namespace Baku.VMagicMirror
{
    [RequireComponent(typeof(Camera))]
    public class BackgroundColorController : MonoBehaviour
    {
        private Camera _camera;

        void Start()
        {
            _camera = GetComponent<Camera>();
        }

        public void ChangeColor(float a, float r, float g, float b)
        {
            _camera.backgroundColor = new Color(r, g, b, a);
        }
    }
}

