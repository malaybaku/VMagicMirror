using UnityEngine;
using UniVRM10;

namespace Baku.VMagicMirror
{
    public class ControlRigVisualizerBone : MonoBehaviour
    {
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private Transform controlBoneVisualizeTransform;
        [SerializeField] private Transform controlTargetVisualizeTransform;

        private Vrm10ControlBone _controlBone;
        private bool _hasBoneParent = false;
        private Transform _boneParent = null;

        public Transform AttachTarget => _controlBone.ControlBone;
        public Transform AttachTargetParent => _controlBone.ControlBone.parent;
        
        public void Attach(Vrm10ControlBone controlBone)
        {
            _controlBone = controlBone;
        }

        private void Update()
        {
            controlBoneVisualizeTransform.SetPositionAndRotation(
                _controlBone.ControlBone.position, _controlBone.ControlBone.rotation
                );

            controlTargetVisualizeTransform.SetPositionAndRotation(
                _controlBone.ControlTarget.position, _controlBone.ControlTarget.rotation
                );
            
            if (!_hasBoneParent)
            {
                lineRenderer.enabled = false;
                return;
            }

            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, AttachTarget.position);
            lineRenderer.SetPosition(1, AttachTargetParent.position);
        }

        public void SetBoneParent(ControlRigVisualizerBone parent)
        {
            _boneParent = parent.transform;
            _hasBoneParent = true;
        }
    }
}
