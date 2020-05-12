using UnityEngine;
using RootMotion.FinalIK;
using Zenject;

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
        [SerializeField] private Vector3 offsetAmplifier = new Vector3(0.2f, 0.2f, 0.5f);
        [SerializeField] private Vector3 offsetLowerLimit = new Vector3(-1.0f, -1.0f, -0.05f);
        [SerializeField] private Vector3 offsetUpperLimit = new Vector3(1.0f, 1.0f, 0.1f);
        [SerializeField] private float speedFactor = 12f;

        [Range(0.05f, 1.0f)]
        [SerializeField] private float timeScaleFactor = 0.3f;

        [SerializeField] private bool enableBodyLeanZ = false;

        [Inject]
        private FaceTracker _faceTracker = null;

        private bool _isVrmLoaded = false;
        private Transform _vrmRoot = null;
        private Transform _leftShoulderEffector = null;
        private Transform _rightShoulderEffector = null;
        private Vector3 _leftShoulderDefaultOffset = Vector3.zero;
        private Vector3 _rightShoulderDefaultOffset = Vector3.zero;

        private Vector3 _prevPosition;
        private Vector3 _prevSpeed;

        private FullBodyBipedIK _ik = null;
        
        /// <summary>体をZ方向に動かしてもよいかどうかを取得、設定します。</summary>
        public bool EnableBodyLeanZ
        {
            get => enableBodyLeanZ;
            set => enableBodyLeanZ = value;
        }
        
        /// <summary>体のIKに適用したいXY軸要素のみが入ったオフセット値を取得します。</summary>
        public Vector3 BodyIkXyOffset { get; private set; }
        
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

        private void Update()
        {
            if (!_isVrmLoaded || !_faceTracker.FaceDetectedAtLeastOnce)
            {
                return;
            }

            float forwardLength = 0.0f;
            float faceSize = _faceTracker.DetectedRect.width * _faceTracker.DetectedRect.height;
            float faceSizeFactor = Mathf.Sqrt(faceSize / _faceTracker.CalibrationData.faceSize);

            //とりあえず簡単に。値域はもっと決めようあるよねここは。
            forwardLength = enableBodyLeanZ
                ? Mathf.Clamp(
                    (faceSizeFactor - 1.0f) * offsetAmplifier.z,
                    offsetLowerLimit.z, 
                    offsetUpperLimit.z
                    )
                : 0f;

            var center = _faceTracker.DetectedRect.center - _faceTracker.CalibrationData.faceCenter;
            var idealPosition = new Vector3(
                center.x * offsetAmplifier.x,
                center.y * offsetAmplifier.y,
                forwardLength
                );

            Vector3 idealSpeed = (idealPosition - _prevPosition) / timeScaleFactor;
            Vector3 speed = Vector3.Lerp(_prevSpeed, idealSpeed, speedFactor * Time.deltaTime);
            Vector3 pos = _prevPosition + Time.deltaTime * speed;

            BodyIkXyOffset = new Vector3(pos.x, pos.y, 0);
            BodyIkOffset = pos;

            var zOffset = _vrmRoot.forward * pos.z;
            //NOTE: IKをこの段階で適用しているが、PositionWeightに依存してホントに見た目に反映されるかが決まる
            _leftShoulderEffector.localPosition = _leftShoulderDefaultOffset + zOffset;
            _rightShoulderEffector.localPosition = _rightShoulderDefaultOffset + zOffset;

            _prevPosition = pos;
            _prevSpeed = speed;
        }

    }
}
