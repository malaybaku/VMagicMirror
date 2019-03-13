using UnityEngine;

namespace Baku.VMagicMirror
{
    [RequireComponent(typeof(Camera))]
    [RequireComponent(typeof(WindowStyleController))]
    public class BackgroundController : MonoBehaviour
    {
        private Camera _camera;
        private WindowStyleController _windowStyleController;

        void Start()
        {
            _camera = GetComponent<Camera>();
            _windowStyleController = GetComponent<WindowStyleController>();
        }

        public void ChangeColor(int a, int r, int g, int b)
        {
            _camera.backgroundColor = new Color(r / 255.0f, g / 255.0f, b / 255.0f, a / 255.0f);
            _windowStyleController.SetWindowTransparency(a == 0);
        }

        public void SetWindowFrameVisibility(bool isVisible) 
            => _windowStyleController.SetWindowFrameVisibility(isVisible);

        public void SetIgnoreMouseInput(bool v) 
            => _windowStyleController.SetIgnoreMouseInput(v);

        public void SetTopMost(bool v)
            => _windowStyleController.SetTopMost(v);

        public void SetWindowDraggable(bool v)
            => _windowStyleController.SetWindowDraggable(v);
    }
}

