using UnityEngine;

namespace Baku.VMagicMirror
{
    public class PresentationWallYawControl : MonoBehaviour
    {
        [SerializeField]
        Transform cam = null;

        void Update()
        {
            if (cam == null) { return; }

            transform.localRotation = Quaternion.AngleAxis(
                cam.rotation.eulerAngles.y + 180,
                Vector3.up
                );
        }
    }
}
