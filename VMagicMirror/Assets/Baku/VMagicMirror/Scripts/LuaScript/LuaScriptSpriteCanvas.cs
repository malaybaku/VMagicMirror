using UnityEngine;

namespace Baku.VMagicMirror.LuaScript
{
    [RequireComponent(typeof(Canvas))]
    public class LuaScriptSpriteCanvas : MonoBehaviour
    {
        [SerializeField] private LuaScriptSpriteInstance scriptSpriteInstancePrefab;
        
        public RectTransform RectTransform => (RectTransform)transform;

        public LuaScriptSpriteInstance CreateInstance() 
            => Instantiate(scriptSpriteInstancePrefab, RectTransform);
    }
}
