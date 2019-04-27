using UnityEngine;
using RootMotion.FinalIK;

namespace Baku.VMagicMirror
{
    public class MotionModifyToMotion : MonoBehaviour
    {
        private Transform _leftArmBendGoal = null;
        private Transform _rightArmBendGoal = null;
        private MotionModifyReceiver _receiver = null;
        private FullBodyBipedIK _ik;

        private void Update() => UpdateElbowParameters();

        public void SetReceiver(MotionModifyReceiver receiver)
        {
            _receiver = receiver;
            UpdateElbowParameters();
        }

        public void InitializeIK(Transform spineBone, FullBodyBipedIK ik)
        {
            _ik = ik;

            _rightArmBendGoal = new GameObject().transform;
            _rightArmBendGoal.SetParent(spineBone);
            _rightArmBendGoal.localRotation = Quaternion.identity;
            _ik.solver.rightArmChain.bendConstraint.bendGoal = _rightArmBendGoal;

            _leftArmBendGoal = new GameObject().transform;
            _leftArmBendGoal.SetParent(spineBone);
            _leftArmBendGoal.localRotation = Quaternion.identity;
            _ik.solver.leftArmChain.bendConstraint.bendGoal = _leftArmBendGoal;

            UpdateElbowParameters();
        }

        private void UpdateElbowParameters()
        {
            if (_receiver == null || _ik == null || _rightArmBendGoal == null || _leftArmBendGoal == null)
            {
                return;
            }

            _ik.solver.rightArmChain.bendConstraint.weight = _receiver.ElbowCloseStrength;
            _ik.solver.leftArmChain.bendConstraint.weight = _receiver.ElbowCloseStrength;

            _rightArmBendGoal.localPosition = new Vector3(_receiver.WaistWidthHalf, 0, 0);
            _leftArmBendGoal.localPosition = new Vector3(-_receiver.WaistWidthHalf, 0, 0);
        }
    }
}

