using System;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    public class FaceAttitudeController : MonoBehaviour
    {
        //NOTE: バネマス系のパラメータ(いわゆるcとk)
        [SerializeField] private Vector3 speedDump = new Vector3(16f, 16f, 16f);
        [SerializeField] private Vector3 posDump = new Vector3(50f, 60f, 60f);

        [Tooltip("Webカメラで得た回転量を角度ごとに強調したり抑えたりするファクター")]
        [SerializeField] private Vector3 headEulerAnglesFactor = Vector3.one;

        //高解像度モードのときに使うc/k/ファクター
        [SerializeField] private Vector3 speedDumpHighPower = new Vector3(15f, 10f, 10f);
        [SerializeField] private Vector3 posDumHighPower = new Vector3(50f, 50f, 50f);
        [SerializeField] private Vector3 headEulerAnglesFactorHighPower = Vector3.one;

        [Tooltip("NeckとHeadが有効なモデルについて、回転を最終的に振り分ける比率を指定します")]
        [Range(0f, 1f)]
        [SerializeField] private float headRate = 0.5f;
        
        [Tooltip("FacePartsが[-1, 1]の範囲で返してくるピッチ、ヨーについて、それらを角度に変換する係数")]
        [SerializeField] private Vector2 facePartsPitchYawFactor = new Vector2(30, 30);

        private FaceTracker _faceTracker = null;
        //private FaceRotToEulerByOpenCVPose _faceRotToEuler = null;
        private FaceRotToEulerByFaceParts _faceRotToEuler = null;

        public event Action<Vector3> ImageBaseFaceRotationUpdated;
        
        [Inject]
        public void Initialize(FaceTracker faceTracker, IVRMLoadable vrmLoadable, IMessageReceiver receiver)
        {
            _faceTracker = faceTracker;
            //_faceRotToEuler = new FaceRotToEulerByOpenCVPose(openCvFacePose);
            _faceRotToEuler = new FaceRotToEulerByFaceParts(_faceTracker.FaceParts);
            
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
            
            receiver.AssignCommandHandler(
                VmmCommands.EnableWebCamHighPowerMode, 
                message => _isHighPowerMode = message.ToBoolean()
                );
        }
        
        //こっちの2つは角度の指定。これらの値もbodyが動くことまで加味して調整
        private const float HeadYawRateToDegFactor = 16.00f;
        
        private const float HeadTotalRotationLimitDeg = 40.0f;

        private const float YawSpeedToPitchDecreaseFactor = 0.05f;
        private const float YawSpeedToPitchDecreaseLimit = 5f;

        private bool _isHighPowerMode = false;

        private bool _hasModel = false;
        private bool _hasNeck = false;
        private Transform _neck = null;
        private Transform _head = null;

        //NOTE: Quaternionを使わないのは角度別にローパスっぽい処理するのに都合がよいため
        private Vector3 _prevRotationEuler;
        private Vector3 _prevRotationSpeedEuler;

        public bool IsActive { get; set; } = true;

        private void LateUpdate()
        {
            if (!(_hasModel && _faceTracker.HasInitDone && IsActive))
            {
                _prevRotationEuler = Vector3.zero;
                _prevRotationSpeedEuler = Vector3.zero;
                ImageBaseFaceRotationUpdated?.Invoke(Vector3.zero);
                return;
            }

            if (_faceTracker.IsHighPowerMode)
            {
                HighPowerModeUpdate();
                return;
            }

            var anglesFactor = _isHighPowerMode ? headEulerAnglesFactorHighPower : headEulerAnglesFactor;
            var speedDumpFactor = _isHighPowerMode ? speedDumpHighPower : speedDump;
            var posDumpFactor = _isHighPowerMode ? posDumHighPower : posDump;

            var target = Mul(
                _faceRotToEuler.GetTargetEulerAngle(
                    facePartsPitchYawFactor, _faceTracker.CalibrationData.pitchRateOffset
                    ),
                anglesFactor
                );

            //やりたい事: バネマス系扱いで陽的オイラー法を回してスムージングする。
            //過減衰方向に寄せてるので雑にやっても大丈夫(のはず)
            var acceleration =
                - Mul(speedDumpFactor, _prevRotationSpeedEuler) -
                Mul(posDumpFactor, _prevRotationEuler - target);
            var speed = _prevRotationSpeedEuler + Time.deltaTime * acceleration;
            var rotationEuler = _prevRotationEuler + speed * Time.deltaTime;

            var rotationAdjusted = new Vector3(
                rotationEuler.x * PitchFactorByYaw(rotationEuler.y) + PitchDiffByYawSpeed(speed.y),
                rotationEuler.y, 
                rotationEuler.z
                );

            //このスクリプトより先にLookAtIKが走るハズなので、その回転と合成していく
            var rot = Quaternion.Euler(rotationAdjusted);

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
            totalHeadRotDeg = Mathf.Repeat(totalHeadRotDeg + 180f, 360f) - 180f;
            
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
            
            ImageBaseFaceRotationUpdated?.Invoke(rotationAdjusted);
        }

        //DEBUGも兼ねて: 高出力モードのとき、一切のスムージングを行わずに頭部姿勢に反映してみる
        private void HighPowerModeUpdate()
        {
            //TODO: 既存姿勢とのブレンディングとか一切考えてないが、本来は必要
            var src = _faceTracker.DnnBasedFaceParts;
            
            var rawRot = Quaternion.Euler(
                src.PitchFactor * 40f,
                src.YawFactor * 30f,
                src.RollAngleDeg
                );

            var rot = Quaternion.Slerp(
                _prevRotation, rawRot, _highPowerSlerpFactor * Time.deltaTime
                );
            _prevRotation = rot;

            rot.ToAngleAxis(out float angle, out var axis);
            rot = Quaternion.AngleAxis(angle * 0.5f, axis);

            _neck.localRotation *= rot;
            _head.localRotation *= rot;
        }

        private float _highPowerSlerpFactor = 12f;
        private Quaternion _prevRotation = Quaternion.identity;
        
        //ヨーの動きがあるとき、首を下に向けさせる(首振り運動は通常ピッチが下がるのを決め打ちでやる)ための処置
        private static float PitchDiffByYawSpeed(float degPerSecond)
        {
            float rate = Mathf.Clamp01(
                Mathf.Abs(degPerSecond) * YawSpeedToPitchDecreaseFactor / YawSpeedToPitchDecreaseLimit
                );
            
            //特にrateが高いほうを丸めるのが狙いです
            return Mathf.SmoothStep(0f, 1f, rate) * YawSpeedToPitchDecreaseLimit;
        }

        //ヨーが0から離れているとき、ピッチを0に近づけるための処置。
        //現行処理だとヨーが0から離れたときピッチが上に寄ってしまうので、それをキャンセルするのが狙い
        private static float PitchFactorByYaw(float yawDeg)
        {
            float rate = Mathf.Clamp01(Mathf.Abs(yawDeg / HeadYawRateToDegFactor));
            return 1.0f - rate * 0.3f;
        }
        
        private static Vector3 Mul(Vector3 left, Vector3 right) => new Vector3(
            left.x * right.x,
            left.y * right.y,
            left.z * right.z
        );
    }
   
    /// <summary>
    /// 頭部の回転がFacePartsに飛んでくるのをオイラー角情報に変換するやつ。
    /// OpenCVPoseを使うケースとコードを揃えるためにこういう書き方
    /// </summary>
    public class FaceRotToEulerByFaceParts
    {
        private readonly FaceParts _faceParts;
        public FaceRotToEulerByFaceParts(FaceParts faceParts)
        {
            _faceParts = faceParts;
        }
        
        public Vector3 GetTargetEulerAngle(Vector2 pitchYawFactor, float pitchRateBaseline)
        {
            float yawAngle = _faceParts.FaceYawRate * pitchYawFactor.y;

            //yawがゼロから離れると幾何的な都合でピッチが大きく出ちゃうので、それの補正
            float dampedPitchRate = _faceParts.FacePitchRate * (1.0f - Mathf.Cos(_faceParts.FaceYawRate) * 0.4f);
            
            float pitchRate = Mathf.Clamp(
                 dampedPitchRate - pitchRateBaseline, -1, 1
                );

            var result = new Vector3(
                pitchRate * pitchYawFactor.x,
                yawAngle,
                _faceParts.FaceRollRad * Mathf.Rad2Deg
                );
            
            return result;
        }
    }

    // NOTE: FacePartsベースの実装に巻き戻したため不要化
    //
    // /// <summary> 頭部の回転がQuaternionで飛んで来るのを安全にオイラー角に変換してくれるやつ </summary>
    // public class FaceRotToEulerByOpenCVPose
    // {
    //     private readonly OpenCVFacePose _facePose;
    //     public FaceRotToEulerByOpenCVPose(OpenCVFacePose facePose)
    //     {
    //         _facePose = facePose;
    //     }
    //     
    //     public Vector3 GetTargetEulerAngle()
    //     {
    //         var rot = _facePose.HeadRotation;
    //         
    //         //安全にやるために、実際に基準ベクトルを回す。処理的には回転行列に置き換えるのに近いかな。
    //         var f = rot * Vector3.forward;
    //         var g = rot * Vector3.right;
    //         
    //         var yaw = Mathf.Asin(f.x) * Mathf.Rad2Deg;
    //         var pitch = -Mathf.Asin(f.y) * Mathf.Rad2Deg;
    //         var roll = Mathf.Asin(g.y) * Mathf.Rad2Deg;
    //
    //         return new Vector3(
    //             NormalRanged(pitch),
    //             NormalRanged(yaw),
    //             NormalRanged(roll)
    //         );
    //     }
    //     
    //     //角度を必ず[-180, 180]の範囲に収めるやつ。この範囲に入ってないとスケーリングとかのときに都合が悪いため。
    //     private static float NormalRanged(float angle)
    //     {
    //         return Mathf.Repeat(angle + 180f, 360f) - 180f;
    //     }
    // }
    
}

