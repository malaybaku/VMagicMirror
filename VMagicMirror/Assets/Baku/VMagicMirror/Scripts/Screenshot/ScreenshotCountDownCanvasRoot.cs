using UnityEngine;

namespace Baku.VMagicMirror
{
    public class ScreenshotCountDownCanvasRoot : MonoBehaviour
    {
        [SerializeField] private ScreenshotCountDownCanvas canvasPrefab = null;

        private bool _hasCanvas = false;
        private ScreenshotCountDownCanvas _canvas = null;

        private ScreenshotCountDownCanvas Canvas
        {
            get
            {
                if (!_hasCanvas)
                {
                    _canvas = Instantiate(canvasPrefab, transform);
                    _hasCanvas = true;
                }
                return _canvas;
            }
        }

        public void Show() => Canvas.Show();
        public void Hide() => Canvas.Hide();
        public void SetCount(int count) => Canvas.SetCount(count);
        public void SetMod(float mod) => Canvas.SetMod(mod);
    }
}