using UnityEngine;
using UnityEngine.UI;

namespace Baku.VMagicMirror
{
    public class ScreenshotCountDownCanvas : MonoBehaviour
    {
        [SerializeField] private Text backgroundText = null;
        [SerializeField] private Text foregroundtext = null;
        
        [SerializeField] private Image image = null;
        private Canvas _canvas = null;
        
        public void Show() => _canvas.gameObject.SetActive(true);
        public void Hide() => _canvas.gameObject.SetActive(false);

        public void SetCount(int count)
        {
            backgroundText.text = count.ToString();
            foregroundtext.text = count.ToString();
        }

        public void SetMod(float mod)
        {
            image.fillAmount = mod;
        }

        private void Start() => _canvas = GetComponent<Canvas>();
    }
}
