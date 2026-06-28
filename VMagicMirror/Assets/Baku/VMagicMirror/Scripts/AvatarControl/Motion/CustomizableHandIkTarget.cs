using UnityEngine;

namespace Baku.VMagicMirror.IK
{
    public class CustomizableHandIkTarget : MonoBehaviour
    {
        [SerializeField] private GameObject handImageGizmo;

        public void SetGizmoImageActiveness(bool active) => handImageGizmo.SetActive(active);
        public Transform TargetTransform => transform;
    }
}
