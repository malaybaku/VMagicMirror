using mattatz.TransformControl;
using UnityEngine;

namespace Baku.VMagicMirror.IK
{
    public class CustomizableHandIkTarget : MonoBehaviour
    {
        [SerializeField] private GameObject handImageGizmo;
        [SerializeField] private TransformControl transformControl;

        public void SetGizmoImageActiveness(bool active) => handImageGizmo.SetActive(active);
        public TransformControl TransformControl => transformControl;
        public Transform TargetTransform => transform;
    }
}
