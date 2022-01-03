using UnityEngine;

namespace mattatz.TransformControl
{
    public class GizmoRenderer : MonoBehaviour
    {
        [SerializeField] private Transform target;
        
        //NOTE: translatesとScaleの内訳は前もって入れておく
        [SerializeField] private GizmoItem translateGizmo;
        [SerializeField] private GizmoItem rotateGizmo;
        [SerializeField] private GizmoItem scaleGizmo;
        [SerializeField] private BoxCollider bounds;

        [SerializeField] private Material xMat;
        [SerializeField] private Material yMat;
        [SerializeField] private Material zMat;
        [SerializeField] private Material selectedMat;
        [SerializeField] private Material mouseOverMat;

        [SerializeField] private Camera targetCamera;

        public Transform Target
        {
            get => target;
            set => target = value;
        }
        public Camera TargetCamera
        {
            get => targetCamera;
            set => targetCamera = value;
        }

        /// <summary>
        /// When set to true, gizmo only for XY plane motion will be visible.
        /// </summary>
        /// <param name="enable"></param>
        public void SetXyPlaneMode(bool enable)
        {
            translateGizmo.SetXyPlaneMode(enable);
            rotateGizmo.SetXyPlaneMode(enable);
            scaleGizmo.SetXyPlaneMode(enable);

        }

        public void SetMode(TransformControl.TransformMode mode)
        {
            translateGizmo.gameObject.SetActive(mode == TransformControl.TransformMode.Translate);
            rotateGizmo.gameObject.SetActive(mode == TransformControl.TransformMode.Rotate);
            scaleGizmo.gameObject.SetActive(mode == TransformControl.TransformMode.Scale);
        }

        public void SetDirection(TransformControl.TransformDirection dir, bool isHover)
        {
            SetItemDirection(translateGizmo, dir, isHover);
            SetItemDirection(rotateGizmo, dir, isHover);
            SetItemDirection(scaleGizmo, dir, isHover);
            
            void SetItemDirection(GizmoItem item, TransformControl.TransformDirection direction, bool hover)
            {
                var validMat = hover ? mouseOverMat : selectedMat;
                item.SetMaterial(AxisTarget.X, direction == TransformControl.TransformDirection.X ? validMat : xMat);
                item.SetMaterial(AxisTarget.Y, direction == TransformControl.TransformDirection.Y ? validMat : yMat);
                item.SetMaterial(AxisTarget.Z, direction == TransformControl.TransformDirection.Z ? validMat : zMat);
            }
        }

        public void SetUseWorldCoord(bool useWorldCoord)
        {
            var rot = useWorldCoord ? Quaternion.identity : Target.rotation;
            translateGizmo.Transform.rotation = rot;
            rotateGizmo.Transform.rotation = rot;
            scaleGizmo.Transform.rotation = rot;
        }

        /// <summary>
        /// Adjust objects' position to keep specific distance from camera
        /// </summary>
        /// <param name="distance"></param>
        public void SetDistance(float distance)
        {
            if (targetCamera == null)
            {
                //It is better to avoid to pass this line
                targetCamera = Camera.main;
            }

            if (targetCamera == null)
            {
                //This line passes only when scene or code setup has some problems
                Debug.LogError("TransformControl cannot find main camera");
                return;
            }

            var camPos = targetCamera.transform.position;
            var dir = (target.position - camPos).normalized;
            transform.position = camPos + dir * distance;
        }

        public void UnsetDistance()
        {
            transform.position = Target.position;
        }

        //Check if mouse cursor might on some gizmos. Useful to reduce redundant hit test calculation
        public bool CheckBoundingBox(Vector3 mouse)
        {
            if (targetCamera == null)
            {
                return false;
            }

            var ray = targetCamera.ScreenPointToRay(mouse);
            return bounds.Raycast(ray, out _, targetCamera.farClipPlane);
        }
    }
}
