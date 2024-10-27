using UnityEngine;
using UnityEngine.UI;

namespace Baku.VMagicMirror.LuaScript
{
    public class LuaScriptSpriteInstance : MonoBehaviour
    {
        [SerializeField] private RawImage rawImage;
        
        public RectTransform RectTransform => (RectTransform)transform;

        public void SetTexture(Texture2D texture) => rawImage.texture = texture;
        
        public void SetPosition(Vector2 position)
        {
            var rt = RectTransform;
            rt.anchorMin = position;
            rt.anchorMax = position;
            rt.anchoredPosition = Vector2.zero;
        }

        public void SetSize(Vector2 size)
        {
            //親のCanvasに対して比率ベースで決めたい
            //正直良くわからんので一旦てきとうにやっています
            RectTransform.sizeDelta = size * 1280;
        }
        
    }
}
