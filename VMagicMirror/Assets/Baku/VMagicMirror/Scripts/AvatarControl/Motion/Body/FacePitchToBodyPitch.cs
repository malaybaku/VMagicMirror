using System.Collections;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>顔のピッチ角を体のピッチ角にすり替えるすごいやつだよ </summary>
    public class FacePitchToBodyPitch : MonoBehaviour
    {
        [Tooltip("最終的に胴体ピッチは頭ピッチの何倍であるべきか、という値")] [SerializeField]
        private float goalRate = -0.05f;

        [Tooltip("ゴールに持ってくときの速度基準にする時定数っぽいやつ")]
        [SerializeField] private float timeFactor = 0.2f;
        
        [Tooltip("ゴール回転値に持ってくとき、スピードに掛けるダンピング項")]
        [UnityEngine.Range(0f, 1f)]
        [SerializeField] private float speedDumpFactor = 0.95f;

        [Tooltip("ゴール回転値に持っていくとき、スピードをどのくらい素早く適用するか")]
        [SerializeField] private float speedLerpFactor = 12.0f;
        
        public Quaternion BodyPitchSuggest { get; private set; } = Quaternion.identity;

        private float _bodyPitchAngleSpeedDegreePerSec = 0;
        private float _bodyPitchAngleDegree = 0;

        private bool _hasVrmBone = false;
        private bool _hasNeck = false;
        private Transform _neck = null;
        private Transform _head = null;
        //NOTE: この値はフィルタされてない生のやつ
        private float _headPitchAngleDegree = 0;
        
        [Inject]
        public void Initialize(IVRMLoadable vrmLoadable)
        {
            vrmLoadable.VrmLoaded += OnVrmLoaded;
            vrmLoadable.VrmDisposing += OnVrmUnloaded;
        }

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            _head = info.animator.GetBoneTransform(HumanBodyBones.Head);
            _neck = info.animator.GetBoneTransform(HumanBodyBones.Neck);
            _hasNeck = _neck != null;

            _headPitchAngleDegree = 0;
            _hasVrmBone = true;
        }
        
        private void OnVrmUnloaded()
        {
            _hasVrmBone = false;
            _hasNeck = false;
            _headPitchAngleDegree = 0;
            _head = null;
            _neck = null;
        }

        private void Start()
        {
            StartCoroutine(CheckHeadPitchAngle());
        }

        private void Update()
        {
            //やること: headRollをbodyRollに変換し、それをQuaternionとして人に見せられる形にする
            float idealSpeed = (_headPitchAngleDegree * goalRate - _bodyPitchAngleDegree) / timeFactor;
            _bodyPitchAngleSpeedDegreePerSec = Mathf.Lerp(
                _bodyPitchAngleSpeedDegreePerSec,
                idealSpeed,
                speedLerpFactor * Time.deltaTime
            );

            _bodyPitchAngleSpeedDegreePerSec *= speedDumpFactor;
            _bodyPitchAngleDegree += _bodyPitchAngleSpeedDegreePerSec * Time.deltaTime;
            
            BodyPitchSuggest = Quaternion.AngleAxis(_bodyPitchAngleDegree, Vector3.right);
            //BodyPitchSuggest = Quaternion.AngleAxis(debugPitchDeg, Vector3.right);
        }

        private IEnumerator CheckHeadPitchAngle()
        {
            var wait = new WaitForEndOfFrame();
            while (true)
            {
                //フレーム終わりでチェックすることで、全ての回転が載った(=描画された)回転値を拾うのが狙いです
                yield return wait;
                if (!_hasVrmBone)
                {
                    _headPitchAngleDegree = 0;
                    continue;
                }

                var headRotation = _hasNeck
                    ? _neck.localRotation * _head.localRotation
                    : _head.localRotation;

                //ピッチはforwardが上がった/下がったの話に帰着すればOK。下向きが正なことに注意
                var rotatedForward = headRotation * Vector3.forward;
                _headPitchAngleDegree = Mathf.Asin(rotatedForward.y) * Mathf.Rad2Deg;
            }
        }
        
    }
}
