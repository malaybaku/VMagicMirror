using UnityEngine;

namespace Baku.VMagicMirror.Buddy
{
    //TODO: Transform2Dの管理都合も踏まえてSpriteCanvasに統合するかも…
    public class BuddyGuiCanvas : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private BuddyGuiAreaInstance guiAreaInstancePrefab;
        
        public RectTransform RectTransform => (RectTransform)transform;

        public BuddyGuiAreaInstance CreateGuiAreaInstance() => Instantiate(guiAreaInstancePrefab, transform);
    }
}
