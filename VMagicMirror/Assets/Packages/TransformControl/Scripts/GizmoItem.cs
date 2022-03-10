using UnityEngine;

namespace mattatz.TransformControl
{
    public enum AxisTarget
    {
        X,
        Y,
        Z,
    }
    
    public class GizmoItem : MonoBehaviour
    {
        [SerializeField] private LineRenderer xAxis;
        [SerializeField] private LineRenderer yAxis;
        [SerializeField] private LineRenderer zAxis;

        [SerializeField] private MeshRenderer xEndEffector;
        [SerializeField] private MeshRenderer yEndEffector;
        [SerializeField] private MeshRenderer zEndEffector;
        [SerializeField] private bool isRotateGizmo;
        
        public Transform Transform => transform;
        
        public void SetMaterial(AxisTarget target, Material mat)
        {
            switch (target)
            {
                case AxisTarget.X:
                    if (!isRotateGizmo)
                    {
                        xAxis.material = mat;
                    }
                    xEndEffector.material = mat;
                    return;
                case AxisTarget.Y:
                    if (!isRotateGizmo)
                    {
                        yAxis.material = mat;
                    }
                    yEndEffector.material = mat;
                    return;
                case AxisTarget.Z:
                    if (!isRotateGizmo)
                    {
                        zAxis.material = mat;
                    }
                    zEndEffector.material = mat;
                    return;
            }
        }
        
        public void SetXyPlaneMode(bool enable)
        {
            if (isRotateGizmo)
            {
                xEndEffector.gameObject.SetActive(!enable);
                yEndEffector.gameObject.SetActive(!enable);
            }
            else
            {
                zAxis.gameObject.SetActive(!enable);
                zEndEffector.gameObject.SetActive(!enable);
            }
        }
    }
}
