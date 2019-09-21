using UnityEngine;
using RootMotion.FinalIK;

namespace Baku.VMagicMirror
{
    //Z方向の制御はVRM側で、肩に対してかける点に注意
    
    /// <summary>
    /// 顔トラッキングの位置から体(胴体)のあるべき位置を補正するやつ
    /// </summary>
    public class ImageBasedBodyMotion : MonoBehaviour
    {
        private const float PositionWeightWhenValid = 0.5f;
        
        //カメラ領域のうち標準では(320x240のうち)30% x 30%の領域くらいが顔の標準の映り込みかなー、という意味。要調整。
        //const float BaseFaceSizeFactor = 0.1f;

        public Vector3 offsetAmplifier = new Vector3(
            0.3f,
            0.3f,
            1.0f
            );

        [SerializeField]
        private Vector3 offsetLowerLimit = new Vector3(
            -1.0f,
            -1.0f,
            -0.05f
            );

        [SerializeField]
        private Vector3 offsetUpperLimit = new Vector3(
            1.0f,
            1.0f,
            0.1f
            );

        [SerializeField] private FaceTracker faceTracker = null;
        [SerializeField] private float speedFactor = 12f;

        [Range(0.05f, 1.0f)]
        [SerializeField]
        private float timeScaleFactor = 0.3f;

        private bool _isVrmLoaded = false;
        private Transform _vrmRoot = null;
        private Transform _leftShoulderEffector = null;
        private Transform _rightShoulderEffector = null;
        private Vector3 _leftShoulderDefaultOffset = Vector3.zero;
        private Vector3 _rightShoulderDefaultOffset = Vector3.zero;

        private Vector3 _prevPosition;
        private Vector3 _prevSpeed;

        private FullBodyBipedIK _ik = null;
        
        /// <summary>体のIKに適用したいオフセット値を取得します。</summary>
        public Vector3 BodyIkOffset { get; private set; }
        
        public void OnVrmLoaded(VrmLoadedInfo info)
        {
            var ik = info.vrmRoot.GetComponent<FullBodyBipedIK>();
            var animator = info.animator;
            _isVrmLoaded = true;
            
            _vrmRoot = animator.transform;

            {
                _leftShoulderEffector = new GameObject("LeftUpperArmEffector").transform;
                _leftShoulderEffector.parent = _vrmRoot;
                
                _leftShoulderDefaultOffset = animator.transform.InverseTransformPoint(
                    animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position
                );
                
                ik.solver.leftShoulderEffector.target = _leftShoulderEffector;
                ik.solver.leftShoulderEffector.positionWeight = PositionWeightWhenValid;
                _leftShoulderEffector.localPosition = _leftShoulderDefaultOffset;
                _leftShoulderEffector.rotation = Quaternion.identity; 
            }
            
            {
                _rightShoulderEffector = new GameObject("RightUpperArmEffector").transform;
                _rightShoulderEffector.parent = _vrmRoot;
                
                _rightShoulderDefaultOffset = animator.transform.InverseTransformPoint(
                    animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position
                );
                ik.solver.rightShoulderEffector.target = _rightShoulderEffector;
                ik.solver.rightShoulderEffector.positionWeight = PositionWeightWhenValid;
                _rightShoulderEffector.localPosition = _rightShoulderDefaultOffset;
                _rightShoulderEffector.rotation = Quaternion.identity;
            }

            _ik = ik;
        }

        public void OnVrmDisposing()
        {
            _isVrmLoaded = false;

            //NOTE: Destroyはしないでもいい(VRM自体のDestroyで勝手に消えるので)
            _ik = null;
            _leftShoulderEffector = null;
            _rightShoulderEffector = null;
        }

        //一気に切り替えるとキモい可能性あるよねコレ

        /// <summary>VRMに顔ベースの移動処理の適用を開始します。</summary>
        public void StartApply()
        {
            if (_ik == null)
            {
                return;
            }

            _ik.solver.rightShoulderEffector.positionWeight = PositionWeightWhenValid;
            _ik.solver.leftShoulderEffector.positionWeight = PositionWeightWhenValid;
        }

        /// <summary>VRMの顔ベースの移動処理の適用を停止します。</summary>
        public void StopApply()
        {
            if (_ik == null)
            {
                return;
            }

            _ik.solver.rightShoulderEffector.positionWeight = 0f;
            _ik.solver.leftShoulderEffector.positionWeight = 0f;
        }

        private void Update()
        {
            if (!_isVrmLoaded || !faceTracker.FaceDetectedAtLeastOnce)
            {
                return;
            }

            float forwardLength = 0.0f;
            float faceSize = faceTracker.DetectedRect.width * faceTracker.DetectedRect.height;
            float faceSizeFactor = Mathf.Sqrt(faceSize / faceTracker.CalibrationData.faceSize);

            //とりあえず簡単に。値域はもっと決めようあるよねここは。
            forwardLength = Mathf.Clamp(
                (faceSizeFactor - 1.0f) * offsetAmplifier.z,
                offsetLowerLimit.z,
                offsetUpperLimit.z
                );

            var center = faceTracker.DetectedRect.center - faceTracker.CalibrationData.faceCenter;
            var idealPosition = new Vector3(
                center.x * offsetAmplifier.x,
                center.y * offsetAmplifier.y,
                forwardLength
                );

            Vector3 idealSpeed = (idealPosition - _prevPosition) / timeScaleFactor;
            Vector3 speed = Vector3.Lerp(_prevSpeed, idealSpeed, speedFactor * Time.deltaTime);
            Vector3 pos = _prevPosition + Time.deltaTime * speed;

            BodyIkOffset = new Vector3(pos.x, pos.y, 0);
            var zOffset = _vrmRoot.forward * pos.z;
            
            //NOTE: IKをこの段階で適用しているが、PositionWeightに依存してホントに見た目に反映されるかが決まる
            _leftShoulderEffector.localPosition = _leftShoulderDefaultOffset + zOffset;
            _rightShoulderEffector.localPosition = _rightShoulderDefaultOffset + zOffset;

            _prevPosition = pos;
            _prevSpeed = speed;
        }

    }
}
