using UnityEngine;
using UnityEngine.UI;

namespace Baku.VMagicMirror
{
    /// <summary> 背景画像を読み込んだ場合のキャンバスの初期化をやるクラス </summary>
    public class BackgroundImageCanvas : MonoBehaviour
    {
        [SerializeField] private Canvas canvas = null;
        [SerializeField] private Image image = null;
        [SerializeField] private AspectRatioFitter fitter = null;
        
        public void SetImage(Texture2D texture)
        {
            if (canvas.worldCamera == null)
            {
                canvas.worldCamera = Camera.main;
            }
            
            if (canvas.worldCamera != null)
            {
                //NOTE: カメラのFar Clipが100とか、そこそこ大きい値であることを前提にした書き方です。
                canvas.planeDistance = canvas.worldCamera.farClipPlane - 1.0f;
            }
            
            image.sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height), 
                new Vector2(0, 0)
            );
            image.preserveAspect = true;
            fitter.aspectRatio = texture.width * 1.0f / texture.height;
            canvas.enabled = true;
        }
    }
}
