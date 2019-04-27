using System;
using UnityEngine;
using UniRx;
using RootMotion.FinalIK;

namespace Baku.VMagicMirror
{
    public class MotionModifyReceiver : MonoBehaviour
    {
        private Transform _leftArmBendGoal = null;
        private Transform _rightArmBendGoal = null;

        private FullBodyBipedIK _ik;

        private float _waistWidthHalf = 0.15f;
        private float _elbowCloseStrength = 0.30f;
        private IDisposable _observer = null;

        private void Awake()
        {
            _leftArmBendGoal = new GameObject().transform;
            _rightArmBendGoal = new GameObject().transform;
        }

        private void Update() => UpdateElbowParameters();
        private void OnDestroy() => _observer?.Dispose();

        public void InitializeIK(Transform spineBone, FullBodyBipedIK ik)
        {
            _ik = ik;

            _rightArmBendGoal.SetParent(spineBone);
            _rightArmBendGoal.localRotation = Quaternion.identity;
            _ik.solver.rightArmChain.bendConstraint.bendGoal = _rightArmBendGoal;

            _leftArmBendGoal.SetParent(spineBone);
            _leftArmBendGoal.localRotation = Quaternion.identity;
            _ik.solver.leftArmChain.bendConstraint.bendGoal = _leftArmBendGoal;

            UpdateElbowParameters();
        }

        public void SetHandler(ReceivedMessageHandler handler)
        {
            _observer?.Dispose();
            _observer = handler.Commands.Subscribe(message =>
            {
                switch (message.Command)
                {
                    case MessageCommandNames.SetWaistWidth:
                        SetWaistWidth(message.ParseAsCentimeter());
                        break;
                    case MessageCommandNames.SetElbowCloseStrength:
                        SetElbowCloseStrength(message.ParseAsPercentage());
                        break;
                    default:
                        break;
                }
            });
        }

        private void UpdateElbowParameters()
        {
            if (_ik == null || _rightArmBendGoal == null || _leftArmBendGoal == null)
            {
                return;
            }

            _ik.solver.rightArmChain.bendConstraint.weight = _elbowCloseStrength;
            _ik.solver.leftArmChain.bendConstraint.weight = _elbowCloseStrength;

            _rightArmBendGoal.localPosition = new Vector3(_waistWidthHalf, 0, 0);
            _leftArmBendGoal.localPosition = new Vector3(-_waistWidthHalf, 0, 0);
        }

        private void SetWaistWidth(float width) => _waistWidthHalf = width * 0.5f;
        private void SetElbowCloseStrength(float strength) => _elbowCloseStrength = strength;
    }
}
