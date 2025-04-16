using Klak.Spout;
using UnityEngine;
using UnityEngine.UI;

namespace Baku.VMagicMirror
{
    public class SpoutSenderWrapperView : MonoBehaviour
    {
        public enum AspectRatioStyle
        {
            /// <summary> 横長 16:9 </summary>
            Landscape,
            /// <summary> 縦長 9:16 </summary>
            Portrait,
        }
        
        [SerializeField] private SpoutSender spoutSender;
        [SerializeField] private Camera windowOverwriteCamera;
        [SerializeField] private Canvas overwriteCanvas;
        [SerializeField] private RawImage overwriteImage;
        [SerializeField] private AspectRatioFitter overwriteImageAspectRatioFitter;

        public void InitializeSpoutSender()
        {
            spoutSender.enabled = false;
            spoutSender.captureMethod = CaptureMethod.Texture;
            spoutSender.sourceTexture = null;
        }
        
        public void SetSpoutSenderActive(bool active) => spoutSender.enabled = active;

        public void SetOverwriteObjectsActive(bool active)
        {
            overwriteCanvas.gameObject.SetActive(active);
            windowOverwriteCamera.gameObject.SetActive(active);
        }

        //NOTE: nullを指定するのもOK
        public void SetTexture(Texture texture)
        {
            spoutSender.sourceTexture = texture;
            overwriteImage.texture = texture;
        }

        public void SetAspectRatioFitterActive(bool active)
        {
            overwriteImageAspectRatioFitter.enabled = active;
            if (!active)
            {
                overwriteImage.transform.localScale = Vector3.one;
                overwriteImage.rectTransform.sizeDelta = Vector2.zero;
            }
        }

        public void SetAspectRatioStyle(AspectRatioStyle style)
        {
            // NOTE: 縦長についてはFitInParentしないと見えが大幅に変わる(めっちゃ拡大されたように見える)ため、
            // 諦めてウィンドウの左右に黒帯が出るようにする
            switch (style)
            {
                case AspectRatioStyle.Landscape:
                    overwriteImageAspectRatioFitter.aspectRatio = 16f / 9f;
                    overwriteImageAspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                    return;
                case AspectRatioStyle.Portrait:
                    overwriteImageAspectRatioFitter.aspectRatio = 9f / 16f;
                    overwriteImageAspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                    return;
            }
        }
    }
}
