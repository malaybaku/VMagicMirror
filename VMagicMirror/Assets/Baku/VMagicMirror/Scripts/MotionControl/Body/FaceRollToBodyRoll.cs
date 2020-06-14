using System.Collections;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 顔のロール回転を体の自然な(無意識運動としての)ロール回転に変換するすごいやつだよ
    /// </summary>
    public class FaceRollToBodyRoll : MonoBehaviour
    {
        [Tooltip("最終的に胴体ロールは頭ロールの何倍であるべきか、という値")]
        [SerializeField] private float goalRate = 0.1f;

        [Tooltip("ゴールに持ってくときの速度基準にする時定数っぽいやつ")]
        [SerializeField] private float timeFactor = 0.3f;
        
        [Tooltip("ゴール回転値に持ってくとき、スピードに掛けるダンピング項")]
        [Range(0f, 1f)]
        [SerializeField] private float speedDumpFactor = 0.95f;

        [Tooltip("ゴール回転値に持っていくとき、スピードをどのくらい素早く適用するか")]
        [SerializeField] private float speedLerpFactor = 18.0f;
        
        public Quaternion BodyRollSuggest { get; private set; } = Quaternion.identity;

        private float _bodyRollAngleSpeedDegreePerSec = 0;
        private float _bodyRollAngleDegree = 0;

        private bool _hasVrmBone = false;
        private bool _hasNeck = false;
        private Transform _neck = null;
        private Transform _head = null;
        //NOTE: この値は
        private float _headRollAngleDegree = 0;
        
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

            _headRollAngleDegree = 0;
            _hasVrmBone = true;
        }
        
        private void OnVrmUnloaded()
        {
            _hasVrmBone = false;
            _hasNeck = false;
            _headRollAngleDegree = 0;
            _head = null;
            _neck = null;
        }

        private void Start()
        {
            StartCoroutine(CheckHeadRollAngle());
        }

        private void Update()
        {
            //やること: headRollをbodyRollに変換し、それをQuaternionとして人に見せられる形にする
            float idealSpeed = (_headRollAngleDegree * goalRate - _bodyRollAngleDegree) / timeFactor;
            _bodyRollAngleSpeedDegreePerSec = Mathf.Lerp(
                _bodyRollAngleSpeedDegreePerSec,
                idealSpeed,
                speedLerpFactor * Time.deltaTime
            );

            _bodyRollAngleSpeedDegreePerSec *= speedDumpFactor;
            _bodyRollAngleDegree += _bodyRollAngleSpeedDegreePerSec * Time.deltaTime;
            
            BodyRollSuggest = Quaternion.AngleAxis(_bodyRollAngleDegree, Vector3.forward);
        }

        private IEnumerator CheckHeadRollAngle()
        {
            var wait = new WaitForEndOfFrame();
            while (true)
            {
                yield return wait;
                //フレーム終わりでチェックすることで、全ての回転が載った(=描画された)回転値を拾うのが狙いです
                if (!_hasVrmBone)
                {
                    _headRollAngleDegree = 0;
                    continue;
                }

                var headRotation = _hasNeck
                    ? _neck.localRotation * _head.localRotation
                    : _head.localRotation;

                //ロールって言ってるけど人間の首は決してUnityのYZXの順で回転するわけじゃないので、実際の計算はファジーにやります。
                //→耳が下とか上を向くのを以て首かしげ運動と見る。
                var rotatedRight = headRotation * Vector3.right;
                _headRollAngleDegree = Mathf.Asin(rotatedRight.y) * Mathf.Rad2Deg;
            }
        }
    }
}
