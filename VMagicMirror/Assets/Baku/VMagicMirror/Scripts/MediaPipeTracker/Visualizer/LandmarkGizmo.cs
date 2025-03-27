using UnityEngine;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    public class LandmarkGizmo : MonoBehaviour
    {
        [SerializeField] private MeshRenderer gizmoRenderer;
        
        // NOTE: Hの上限をあえて0.8で切っているのは index = 0 付近と index = totalCount 付近の見分けがつくようにするため
        public void SetIndex(int index, int totalCount) 
            => gizmoRenderer.material.color = Color.HSVToRGB(index * 0.8f / totalCount, 1f, 1f);

        public void SetPosition(Vector3 position) => transform.localPosition = position;

        public void SetLocalPosition(Vector3 scale) => transform.localScale = scale;
    }
}
