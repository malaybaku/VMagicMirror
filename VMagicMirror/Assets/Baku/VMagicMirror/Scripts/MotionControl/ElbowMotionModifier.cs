using UnityEngine;
using RootMotion.FinalIK;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// ひじをちょっと内側に折りたたむための処置。
    /// </summary>
    public class ElbowMotionModifier : MonoBehaviour
    {
        [SerializeField] private ElbowMotionModifyReceiver receiver = null;
        [Inject] private IVRMLoadable _vrmLoadable = null;

        private bool _isInitialized = false;
        private Transform _leftArmBendGoal = null;
        private Transform _rightArmBendGoal = null;
        private FullBodyBipedIK _ik;

        /// <summary>
        /// IKの効きを補正するファクターで0から1の値を指定します。
        /// </summary>
        public float ElbowIkRate { get; set; } = 1.0f;

        private void Start()
        {
            _vrmLoadable.VrmLoaded += OnVrmLoaded;
            _vrmLoadable.VrmDisposing += OnVrmDisposing;
        }

        private void Update()
        {
            if (!_isInitialized)
            {
                return;
            }

            _ik.solver.rightArmChain.bendConstraint.weight = receiver.ElbowCloseStrength * ElbowIkRate;
            _ik.solver.leftArmChain.bendConstraint.weight = receiver.ElbowCloseStrength * ElbowIkRate;

            _rightArmBendGoal.localPosition = new Vector3(receiver.WaistWidthHalf, 0, 0);
            _leftArmBendGoal.localPosition = new Vector3(-receiver.WaistWidthHalf, 0, 0);            
        }

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            _ik = info.vrmRoot.GetComponent<FullBodyBipedIK>();
            var spineBone = info.animator.GetBoneTransform(HumanBodyBones.Spine);

            _rightArmBendGoal = new GameObject().transform;
            _rightArmBendGoal.SetParent(spineBone);
            _rightArmBendGoal.localRotation = Quaternion.identity;
            _ik.solver.rightArmChain.bendConstraint.bendGoal = _rightArmBendGoal;

            _leftArmBendGoal = new GameObject().transform;
            _leftArmBendGoal.SetParent(spineBone);
            _leftArmBendGoal.localRotation = Quaternion.identity;
            _ik.solver.leftArmChain.bendConstraint.bendGoal = _leftArmBendGoal;
            
            _isInitialized = true;
        }

        private void OnVrmDisposing()
        {
            _ik = null;
            _rightArmBendGoal = null;
            _leftArmBendGoal = null;
            _isInitialized = false;
        }
    }
}