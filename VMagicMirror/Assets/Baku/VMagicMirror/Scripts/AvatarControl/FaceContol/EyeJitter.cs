using UnityEngine;
using Zenject;

//ref: https://gist.github.com/GOROman/51ee32887bd1d3248b7610f845904b30
namespace Baku.VMagicMirror
{
    /// <summary>
    /// 眼球微細運動をするやつ。参照元に対して、ランタイムのVRMロードを想定した処理が増えてるのが最大の変更点
    /// </summary>
    public class EyeJitter : MonoBehaviour
    {
        [Tooltip("Jitterの回転値が変化する最小の時間間隔")]
        [SerializeField] private float changeTime = 0.4f;
        
        [Tooltip("Jitterの回転値が変化する最大の時間間隔")]
        [SerializeField] private float changeTimeRange = 2.0f;
        
        [Tooltip("可動範囲")]
        [SerializeField] private Vector2 range = new Vector2(0.001f, 0.03f);

        [Tooltip("微細運動をスムージングする速度ファクタ")]
        [SerializeField] private float speedFactor = 11.0f;

        [Inject] private IVRMLoadable _vrmLoadable = null;

        private Transform _rightEye = null;
        private Transform _leftEye = null;
        private bool _hasValidEyeBone = false;

        private float _count = 0.0f;
        private Quaternion _targetRotation;
        public Quaternion CurrentRotation { get; private set; }
        
        public bool IsActive { get; set; }
        
        private void Start()
        {
            _vrmLoadable.VrmLoaded += OnVrmLoaded;
            _vrmLoadable.VrmDisposing += OnVrmDisposing;
        }

        private void LateUpdate()
        {
            _count -= Time.deltaTime;
            if (_count < 0)
            {
                _count = Random.Range(changeTime, changeTimeRange);
                
                float eulerAngleX = Random.Range(-range.x, +range.x);
                float eulerAngleY = Random.Range(-range.y, +range.y);
                _targetRotation = Quaternion.Euler(eulerAngleX, eulerAngleY, 0);
            }

            CurrentRotation = Quaternion.Slerp(
                CurrentRotation,
                _targetRotation,
                speedFactor * Time.deltaTime
                );
            
            if (_hasValidEyeBone && IsActive)
            {
                //この呼び出しより前の時点でVRMLookAtが毎フレームEyeの位置をいい感じにするため、毎フレームごとに補正してればOK
                _rightEye.localRotation *= CurrentRotation;
                _leftEye.localRotation *= CurrentRotation;
            }
        }
        
        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            _rightEye = info.animator.GetBoneTransform(HumanBodyBones.RightEye);
            _leftEye = info.animator.GetBoneTransform(HumanBodyBones.LeftEye);
            _hasValidEyeBone = (_rightEye != null && _leftEye != null);
        }

        private void OnVrmDisposing()
        {
            _rightEye = null;
            _leftEye = null;
            _hasValidEyeBone = false;
        }
    }
}
