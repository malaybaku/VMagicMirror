using UnityEngine;
using UnityEngine.UI;

namespace Baku.VMagicMirror
{
    public class ScreenshotCountDownCanvas : MonoBehaviour
    {
        [SerializeField] private Text backgroundText = null;
        [SerializeField] private Text foregroundtext = null;
        
        [SerializeField] private Image image = null;
        [SerializeField] private Canvas canvas = null;
        
        public void Show() => canvas.gameObject.SetActive(true);
        public void Hide() => canvas.gameObject.SetActive(false);

        public void SetCount(int count)
        {
            backgroundText.text = count.ToString();
            foregroundtext.text = count.ToString();
        }

        public void SetMod(float mod)
        {
            image.fillAmount = mod;
        }
    }
}
