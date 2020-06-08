using System.Collections;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 画像ベースの顔のヨー回転を体の自然な(無意識運動としての)ヨー回転に変換するすごいやつだよ
    /// </summary>
    public class FaceYawToBodyYaw : MonoBehaviour
    {
        [Tooltip("最終的に胴体ヨーは頭ヨーの何倍であるべきか、という値")]
        [SerializeField] private float goalRate = 0.3f;

        [Tooltip("ゴールに持ってくときの速度基準にする時定数っぽいやつ")]
        [SerializeField] private float timeFactor = 0.3f;
        
        [Tooltip("ゴール回転値に持ってくとき、スピードに掛けるダンピング項")]
        [Range(0f, 1f)]
        [SerializeField] private float speedDumpFactor = 0.95f;

        [Tooltip("ゴール回転値に持っていくとき、スピードをどのくらい素早く適用するか")]
        [SerializeField] private float speedLerpFactor = 18.0f;
        
        public Quaternion BodyYawSuggest { get; private set; } = Quaternion.identity;

        private float _bodyYawAngleSpeedDegreePerSec = 0;
        private float _bodyYawAngleDegree = 0;

        private bool _hasVrmBone = false;
        private Transform _head = null;
        //NOTE: この値は
        private float _headYawAngleDegree = 0;
        
        [Inject]
        public void Initialize(IVRMLoadable vrmLoadable)
        {
            vrmLoadable.VrmLoaded += OnVrmLoaded;
            vrmLoadable.VrmDisposing += OnVrmUnloaded;
        }

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            _head = info.animator.GetBoneTransform(HumanBodyBones.Head);
            _headYawAngleDegree = 0;
            _hasVrmBone = true;
        }
        
        private void OnVrmUnloaded()
        {
            _hasVrmBone = false;
            _headYawAngleDegree = 0;
            _head = null;
        }

        private void Start()
        {
            StartCoroutine(CheckHeadYawAngle());
        }

        private void Update()
        {
            //やること: headYawをbodyYawに変換し、それをQuaternionとして人に見せられる形にする
            float idealSpeed = (_headYawAngleDegree * goalRate - _bodyYawAngleDegree) / timeFactor;
            _bodyYawAngleSpeedDegreePerSec = Mathf.Lerp(
                _bodyYawAngleSpeedDegreePerSec,
                idealSpeed,
                speedLerpFactor * Time.deltaTime
            );

            _bodyYawAngleSpeedDegreePerSec *= speedDumpFactor;
            _bodyYawAngleDegree += _bodyYawAngleSpeedDegreePerSec * Time.deltaTime;
            
            BodyYawSuggest = Quaternion.AngleAxis(_bodyYawAngleDegree, Vector3.up);
        }

        private IEnumerator CheckHeadYawAngle()
        {
            var wait = new WaitForEndOfFrame();
            while (true)
            {
                yield return wait;
                //フレーム終わりでチェックすることで、全ての回転が載った(=描画された)回転値を拾うのが狙いです
                if (!_hasVrmBone)
                {
                    _headYawAngleDegree = 0;
                    continue;
                }

                var headForward = _head.forward;
                _headYawAngleDegree = -(Mathf.Atan2(headForward.z, headForward.x) * Mathf.Rad2Deg - 90);
            }
        }
    }
}
