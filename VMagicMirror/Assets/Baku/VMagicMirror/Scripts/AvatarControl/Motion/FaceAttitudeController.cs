using System;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    //NOTE: 筋肉的には「首が横に振るときはピッチを下げるべき」みたいな解釈もあるけど一旦やらない
    public class FaceAttitudeController : MonoBehaviour
    {
        //NOTE: 頭部運動はどう早く見てもせいぜい2hzくらい、と考えつつ、カットオフを妥協して
        //ゆっくりの動作は0.5hzくらいと考えられるが、1fにしておくとslow/quickの差が減るのがメリット
        private const float CutOffFrequencySlow = 1f;
        private const float CutOffFrequencyQuick = 1.5f;
        //カットオフ周波数を一気に切り替えるとスナップ運動が発生して不自然なので、何fか使って遷移させる￥
        private const float CutOffFrequencyChangeSpeedPerSec = 6f;

        //頭の回転限界: bodyも動くことまで踏まえて調整してることに注意。つまり頭のワールド回転の上限はもっと大きい
        private const float HeadTotalRotationLimitDeg = 35.0f;
        

        [Tooltip("Webカメラで得た回転量を角度ごとに強調したり抑えたりするファクター")]
        [SerializeField] private Vector3 headEulerAnglesFactor = new Vector3(1.2f, 1.3f, 1f);
        
        [Tooltip("NeckとHeadが有効なモデルについて、回転を最終的に振り分ける比率を指定します")] 
        [Range(0f, 1f)] 
        [SerializeField] private float headRate = 0.5f;

        [Tooltip("FacePartsが[-1, 1]の範囲で返してくるピッチ、ヨーについて、それらを角度に変換する係数")] 
        [SerializeField] private Vector2 facePartsPitchYawFactor = new Vector2(30, 30);

        //この角度を超えてtargetとフィルタ後の角度がズレ続けた場合、すばやく動くモードに切り替える
        //pitchにデカい値が入ってるのはpitchを信用しない、というくらいの意味
        [SerializeField] private Vector3 deadZoneLeaveRange = new Vector3(50f, 10f, 10f);
        [SerializeField] private int deadZoneLeaveCount = 3;

        //この角度以内にtargetとフィルタ後の角度が入り続けた場合、ゆっくり動くモードに切り替える
        //pitchにデカい値が入ってるのはpitchを信用しない、というくらいの意味
        [SerializeField] private Vector3 deadZoneEnterRange = new Vector3(50f, 6f, 6f);
        [SerializeField] private int deadZoneEnterCount = 6;

        private FaceTracker _faceTracker;
        private GameInputBodyMotionController _gameInputBodyMotionController;
        private CarHandleBasedFK _carHandleBasedFk;

        public event Action<Vector3> ImageBaseFaceRotationUpdated;

        [Inject]
        public void Initialize(
            //FaceTracker faceTracker,
            GameInputBodyMotionController gameInputBodyMotionController,
            CarHandleBasedFK carHandleBasedFk,
            IVRMLoadable vrmLoadable)
        {
            //_faceTracker = faceTracker;
            _gameInputBodyMotionController = gameInputBodyMotionController;
            _carHandleBasedFk = carHandleBasedFk;

            vrmLoadable.VrmLoaded += info =>
            {
                var animator = info.controlRig;
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
        
        private bool _hasModel = false;
        private bool _hasNeck = false;
        private Transform _neck = null;
        private Transform _head = null;
        
        public bool IsActive { get; set; } = true;

        private readonly BiQuadFilterVector3 _anglesFilter = new BiQuadFilterVector3();

        // すばやく動くモードのときはtrue
        private bool _isQuickMode;
        private int _deadZoneSwitchCount = 0;
        private float _cutOffFrequency = 0.5f;

        private void Start() => ApplyCutOffFrequencyImmediate();

        private void LateUpdate()
        {
            if (!(_hasModel && _faceTracker.HasInitDone && IsActive))
            {
                if (_isQuickMode)
                {
                    _isQuickMode = false;
                    ApplyCutOffFrequencyImmediate();
                }
                _anglesFilter.ResetValue(Vector3.zero);
                ImageBaseFaceRotationUpdated?.Invoke(Vector3.zero);
                return;
            }

            var rawTarget = GetLowPowerModeEulerAngle(_faceTracker.CurrentAnalyzer.Result, facePartsPitchYawFactor);
         
            var target = Vector3.Scale(rawTarget, headEulerAnglesFactor);

            var rotationAdjusted = _anglesFilter.Update(target);
            CheckDeadZone(rotationAdjusted - target);
            UpdateCutOffFrequency();
            //NOTE: 30FPSの場合、60FPS用のフィルタリングを2回まわすと整合するので、雑にコレで済ませる
            if (Time.deltaTime > 0.025f)
            {
                rotationAdjusted = _anglesFilter.Update(target);
                CheckDeadZone(rotationAdjusted - target);
                UpdateCutOffFrequency();
            }
            
            //このスクリプトより先にLookAtIKが走るハズなので、その回転と合成していく
            ApplyRotationToHeadBone(rotationAdjusted);
        }

        //デッドゾーンから入ったり抜けたりの状態を管理します。
        private void CheckDeadZone(Vector3 diff)
        {
            if (_isQuickMode)
            {
                if (Mathf.Abs(diff.x) < deadZoneEnterRange.x &&
                    Mathf.Abs(diff.y) < deadZoneEnterRange.y &&
                    Mathf.Abs(diff.z) < deadZoneEnterRange.z)
                {
                    _deadZoneSwitchCount++;
                    if (_deadZoneSwitchCount >= deadZoneEnterCount)
                    {
                        _deadZoneSwitchCount = 0;
                        _isQuickMode = false;
                    }
                }
                else
                {
                    _deadZoneSwitchCount = 0;
                }
            }
            else
            {
                if (Mathf.Abs(diff.x) > deadZoneLeaveRange.x ||
                    Mathf.Abs(diff.y) > deadZoneLeaveRange.y ||
                    Mathf.Abs(diff.z) > deadZoneLeaveRange.z)
                {
                    _deadZoneSwitchCount++;
                    if (_deadZoneSwitchCount >= deadZoneLeaveCount)
                    {
                        _deadZoneSwitchCount = 0;
                        _isQuickMode = true;
                    }
                }
                else
                {
                    _deadZoneSwitchCount = 0;
                }
            }
        }

        private void ApplyCutOffFrequencyImmediate()
        {
            _cutOffFrequency = _isQuickMode ? CutOffFrequencyQuick : CutOffFrequencySlow;
            _anglesFilter.SetUpAsLowPassFilter(60f, _cutOffFrequency * Vector3.one);
        }

        //カットオフ周波数が目標値より大きい/少ないに応じて周波数自体をいじる
        private void UpdateCutOffFrequency()
        {
            float targetFreq = _isQuickMode ? CutOffFrequencyQuick : CutOffFrequencySlow;
            if (Mathf.Abs(_cutOffFrequency - targetFreq) < 0.001f)
            {
                return;
            }

            if (_cutOffFrequency < targetFreq)
            {
                _cutOffFrequency = Mathf.Min(
                    _cutOffFrequency + CutOffFrequencyChangeSpeedPerSec * Time.deltaTime,
                    CutOffFrequencyQuick
                );
            }
            else
            {
                _cutOffFrequency = Mathf.Max(
                    _cutOffFrequency - CutOffFrequencyChangeSpeedPerSec * Time.deltaTime,
                    CutOffFrequencySlow
                );
            }
            _anglesFilter.SetUpAsLowPassFilter(60f, _cutOffFrequency * Vector3.one);
        }
        
        private void ApplyRotationToHeadBone(Vector3 rotationEuler)
        {
            var rot =
                _gameInputBodyMotionController.LookAroundRotation * 
                _carHandleBasedFk.GetHeadYawRotation() *
                Quaternion.Euler(rotationEuler);
            
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
            
            ImageBaseFaceRotationUpdated?.Invoke(rotationEuler);   
        }
        
        // 頭部の回転をdegベースのオイラー角情報に変換する (低負荷モード用)
        private static Vector3 GetLowPowerModeEulerAngle(
            IFaceAnalyzeResult faceAnalyzeResult, Vector2 pitchYawFactor
            )
        {
            var yawRate = Mathf.Clamp(faceAnalyzeResult.YawRate, -1f, 1f);

            //yawがゼロから離れると幾何的な都合でピッチが大きく出ちゃうので、それの補正
            float dampedPitchRate = faceAnalyzeResult.PitchRate * (1.0f - Mathf.Cos(yawRate) * 0.4f);

            float pitchRate = Mathf.Clamp(dampedPitchRate, -1, 1);

            var result = new Vector3(
                pitchRate * pitchYawFactor.x,
                yawRate * pitchYawFactor.y,
                faceAnalyzeResult.RollRad * Mathf.Rad2Deg
            );

            return result;
        }
    }
}

