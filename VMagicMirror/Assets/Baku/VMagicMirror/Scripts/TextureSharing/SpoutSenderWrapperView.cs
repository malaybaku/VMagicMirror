using Klak.Spout;
using UnityEngine;
using UnityEngine.UI;

namespace Baku.VMagicMirror
{
    public class SpoutSenderWrapperView : MonoBehaviour
    {
        [SerializeField] private SpoutSender spoutSender;
        [SerializeField] private Camera windowOverwriteCamera;
        [SerializeField] private Canvas overwriteCanvas;
        [SerializeField] private RawImage overwriteImage;

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
    }
}
