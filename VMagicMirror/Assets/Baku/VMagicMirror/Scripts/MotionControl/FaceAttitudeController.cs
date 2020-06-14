using UnityEngine;
using UniRx;
using Zenject;

namespace Baku.VMagicMirror
{
    public class FaceAttitudeController : MonoBehaviour
    {
        //NOTE: バネマス系のパラメータ(いわゆるcとk)
        [SerializeField] private float speedDumpForceFactor = 10.0f;
        [SerializeField] private float posDumpForceFactor = 20.0f;

        [Tooltip("NeckとHeadが有効なモデルについて、回転を最終的に振り分ける比率を指定します")]
        [Range(0f, 1f)]
        [SerializeField] private float headRate = 0.5f;
        
        private FaceTracker _faceTracker = null;
        
        [Inject]
        public void Initialize(FaceTracker faceTracker, IVRMLoadable vrmLoadable)
        {
            _faceTracker = faceTracker;
            
            //鏡像姿勢をベースにしたいので反転(この値を適用するとユーザーから鏡に見えるハズ)
            faceTracker.FaceParts.Outline.HeadRollRad.Subscribe(
                v => SetHeadRollDeg(-v * Mathf.Rad2Deg * HeadRollRateApplyFactor)
            );
            
            //もとの値は角度ではなく[-1, 1]の無次元量であることに注意
            faceTracker.FaceParts.Outline.HeadYawRate.Subscribe(
                v => SetHeadYawDeg(v * HeadYawRateToDegFactor)
            );

            faceTracker.FaceParts.Nose.NoseBaseHeightValue.Subscribe(
                v => SetHeadPitchDeg(NoseBaseHeightToNeckPitchDeg(v))
            );
            
            vrmLoadable.VrmLoaded += info =>
            {
                var animator = info.animator;
                _neck = animator.GetBoneTransform(HumanBodyBones.Neck);
                _head = animator.GetBoneTransform(HumanBodyBones.Head);
                _hasNeck = (_neck != null);
                _hasModel = true;
            };
            
            vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _hasNeck = false;
                _neck = null;
                _head = null;
            };
        }
        
        //体の回転に反映するとかの都合で首ロールを実際に検出した値より控えめに適用しますよ、というファクター
        private const float HeadRollRateApplyFactor = 0.8f;
        //NOTE: もとは50だったんだけど、腰曲げに反映する値があることを想定して小さくしてます
        private const float HeadYawRateToDegFactor = 28.00f; 

        private const float HeadTotalRotationLimitDeg = 40.0f;
        private const float NoseBaseHeightDifToAngleDegFactor = 300f;

        private bool _hasModel = false;
        private bool _hasNeck = false;
        private Transform _neck = null;
        private Transform _head = null;

        private void SetHeadRollDeg(float value) => _latestRotationEuler.z = value;
        private void SetHeadYawDeg(float value) => _latestRotationEuler.y = value;
        private void SetHeadPitchDeg(float value) => _latestRotationEuler.x = value;

        //NOTE: Quaternionを使わないのは角度別にローパスっぽい処理するのに都合がよいため
        private Vector3 _latestRotationEuler;
        private Vector3 _prevRotationEuler;
        private Vector3 _prevRotationSpeedEuler;

        public bool IsActive { get; set; } = true;

        private void LateUpdate()
        {
            if (!(_hasModel && _faceTracker.HasInitDone && IsActive))
            {
                _latestRotationEuler = Vector3.zero;
                _prevRotationEuler = Vector3.zero;
                _prevRotationSpeedEuler = Vector3.zero;
                return;
            }

            //やりたい事: ロール、ヨー、ピッチそれぞれを独立にsmoothingしてから最終的に適用する
            // いわゆるバネマス系扱いで陽的オイラー法を回す。やや過減衰方向に寄せてるので雑にやっても大丈夫(のはず)
            var accel = 
                -_prevRotationSpeedEuler * speedDumpForceFactor -
                (_prevRotationEuler - _latestRotationEuler) * posDumpForceFactor;
            var speed = _prevRotationSpeedEuler + Time.deltaTime * accel;
            var rotationEuler = _prevRotationEuler + speed * Time.deltaTime;
            
            //このスクリプトより先にLookAtIKが走るハズなので、その回転と合成していく
            var rot = Quaternion.Euler(rotationEuler);

            //特に首と頭を一括で回すにあたって、コーナーケースを安全にするため以下のアプローチを取る
            // - 一旦今の回転値を混ぜて、
            // - 角度制限つきの最終的な回転値を作り、
            // - その回転値を角度ベースで首と頭に配り直す
            var totalRot = _hasNeck
                ? rot * _neck.localRotation * _head.localRotation
                : rot * _head.localRotation;
            
            totalRot.ToAngleAxis(
                out float totalHeadRotDeg,
                out Vector3 totalHeadRotAxis
            );
            
            //素朴に値を適用すると首が曲がりすぎる、と判断されたケース
            if (Mathf.Abs(totalHeadRotDeg) > HeadTotalRotationLimitDeg)
            {
                totalHeadRotDeg = Mathf.Sign(totalHeadRotDeg) * HeadTotalRotationLimitDeg;
            }

            if (_hasNeck)
            {
                _neck.localRotation = Quaternion.AngleAxis(totalHeadRotDeg * (1 - headRate), totalHeadRotAxis);
                _head.localRotation = Quaternion.AngleAxis(totalHeadRotDeg * headRate, totalHeadRotAxis);
            }
            else
            {
                _head.localRotation = Quaternion.AngleAxis(totalHeadRotDeg, totalHeadRotAxis);
            }
            
            _prevRotationEuler = rotationEuler;
            _prevRotationSpeedEuler = speed;
        }
 
        private float NoseBaseHeightToNeckPitchDeg(float noseBaseHeight)
        {
            if (_faceTracker != null)
            {
                return -(noseBaseHeight - _faceTracker.CalibrationData.noseHeight) * NoseBaseHeightDifToAngleDegFactor;
            }
            else
            {
                //とりあえず顔が取れないなら水平にしとけばOK
                return 0;
            }
        }
    }
}

