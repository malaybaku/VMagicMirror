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
        [SerializeField] private float timeFactor = 0.15f;
        
        [Tooltip("ゴール回転値に持ってくとき、スピードに掛けるダンピング項")]
        [Range(0f, 1f)]
        [SerializeField] private float speedDumpFactor = 0.98f;

        [Tooltip("ゴール回転値に持っていくとき、スピードをどのくらい素早く適用するか")]
        [SerializeField] private float speedLerpFactor = 12.0f;
        
        public Quaternion BodyYawSuggest { get; private set; } = Quaternion.identity;

        private float _bodyYawAngleSpeedDegreePerSec = 0;
        private float _bodyYawAngleDegree = 0;

        private bool _hasVrmBone = false;
        private bool _hasNeck = false;
        private Transform _head = null;
        private Transform _neck = null;
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
            _neck = info.animator.GetBoneTransform(HumanBodyBones.Neck);
            _headYawAngleDegree = 0;
            _hasNeck = (_neck != null);
            _hasVrmBone = true;
        }
        
        private void OnVrmUnloaded()
        {
            _hasVrmBone = false;
            _hasNeck = false;
            _headYawAngleDegree = 0;
            _head = null;
            _neck = null;
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
                
                var headRotation = _hasNeck
                    ? _neck.localRotation * _head.localRotation
                    : _head.localRotation;

                //首の回転ベースで正面向きがどうなったか見る: コレでうまく動きます
                var headForward = headRotation * Vector3.forward;
                _headYawAngleDegree = -(Mathf.Atan2(headForward.z, headForward.x) * Mathf.Rad2Deg - 90);
            }
        }
    }
}
