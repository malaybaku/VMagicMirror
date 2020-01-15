using System;
using UnityEngine;
using UniRx;
using VRM;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary> 瞬きに対して目と眉を下げる処理をするやつ </summary>
    public class EyeDownBlendShapeController : MonoBehaviour
    {
        private static readonly BlendShapeKey BlinkLKey = new BlendShapeKey(BlendShapePreset.Blink_L);
        private static readonly BlendShapeKey BlinkRKey = new BlendShapeKey(BlendShapePreset.Blink_R);

        [SerializeField] private FaceControlManager faceControlManager = null;
        [SerializeField] private WordToMotionManager wordToMotion = null;

        //ちょっとデフォルトで眉を上げとこう的な値。目の全開きは珍しいという仮説による。
        [SerializeField] private float defaultOffset = 0.2f;

        [SerializeField] private float eyeBrowDownOffsetWhenEyeClosed = 0.7f;

        [SerializeField] private float eyeAngleDegreeWhenEyeClosed = 10f;

        [SerializeField] private float speedLerpFactor = 0.2f;
        [SerializeField] [Range(0.05f, 1.0f)] private float timeScaleFactor = 0.3f;

        [Inject] private FaceTracker _faceTracker = null;
        [Inject] private IVRMLoadable _vrmLoadable = null;

        private EyebrowBlendShapeSet EyebrowBlendShape => faceControlManager.EyebrowBlendShape;

        private VRMBlendShapeProxy _blendShapeProxy = null;
        private Transform _rightEyeBone = null;
        private Transform _leftEyeBone = null;

        private float _rightEyeBrowValue = 0.0f;
        private float _leftEyeBrowValue = 0.0f;

        //単位: %
        private float _prevLeftEyeBrowWeight = 0;

        private float _prevRightEyeBrowWeight = 0;

        //単位: %/s
        private float _prevLeftEyeBrowSpeed = 0;
        private float _prevRightEyeBrowSpeed = 0;

        //このクラス上でリセットされていないBlendShapeを送った状態かどうかのフラグ。
        private bool _hasAppliedEyebrowBlendShape = false;

        private IDisposable _rightEyeBrowHeight = null;
        private IDisposable _leftEyeBrowHeight = null;

        //「目ボーンがある + まばたきブレンドシェイプがある」の2つで判定
        private bool _hasValidEyeSettings = false;

        public bool IsInitialized { get; private set; } = false;

        /// <summary>
        /// 顔トラッキング時にも自動まばたきを優先するかどうか
        /// </summary>
        public bool PreferAutoBlink { get; set; } = false;

        private void Start()
        {
            _vrmLoadable.VrmLoaded += OnVrmLoaded;
            _vrmLoadable.VrmDisposing += OnVrmDisposing;
        }
        
        private void LateUpdate()
        {
            if (!IsInitialized)
            {
                return;
            }

            AdjustEyeRotation();
            AdjustEyebrow();
        }

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            _blendShapeProxy = info.blendShape;
            _rightEyeBone = info.animator.GetBoneTransform(HumanBodyBones.RightEye);
            _leftEyeBone = info.animator.GetBoneTransform(HumanBodyBones.LeftEye);

            _rightEyeBrowHeight?.Dispose();
            _rightEyeBrowHeight = _faceTracker.FaceParts.RightEyebrow.Height.Subscribe(
                v => _rightEyeBrowValue = v
            );

            _leftEyeBrowHeight?.Dispose();
            _leftEyeBrowHeight = _faceTracker.FaceParts.LeftEyebrow.Height.Subscribe(
                v => _leftEyeBrowValue = v
            );

            _hasValidEyeSettings =
                _rightEyeBone != null &&
                _leftEyeBone != null &&
                CheckBlinkBlendShapeClips(_blendShapeProxy);

            IsInitialized = true;
        }

        private void OnVrmDisposing()
        {
            _blendShapeProxy = null;
            _rightEyeBone = null;
            _leftEyeBone = null;
            _hasValidEyeSettings = false;

            _rightEyeBrowHeight?.Dispose();
            _rightEyeBrowHeight = null;

            _leftEyeBrowHeight?.Dispose();
            _leftEyeBrowHeight = null;

            IsInitialized = false;
        }

        private void AdjustEyeRotation()
        {
            //NOTE: どっちかというとWordToMotion用に"Disable/Enable"系のAPI出す方がいいかも
            if (!_hasValidEyeSettings ||
                wordToMotion.EnablePreview ||
                wordToMotion.IsPlayingBlendShape)
            {
                return;
            }

            float leftBlink = _blendShapeProxy.GetValue(BlinkLKey);
            float rightBlink = _blendShapeProxy.GetValue(BlinkRKey);

            //NOTE: 毎回LookAtで値がうまく設定されてる前提でこういう記法になっている事に注意
            _leftEyeBone.localRotation *= Quaternion.AngleAxis(
                eyeAngleDegreeWhenEyeClosed * leftBlink,
                Vector3.right
            );

            _rightEyeBone.localRotation *= Quaternion.AngleAxis(
                eyeAngleDegreeWhenEyeClosed * rightBlink,
                Vector3.right
            );
        }

        private void AdjustEyebrow()
        {
            //プレビューやクリップで指定されたモーフの実行時: 眉毛が動いてるとジャマなので戻してから放置。
            //毎フレームやるとクリップ指定動作を上書きしてしまうため、それを防ぐべく最初の1回だけリセットするのがポイント
            if (wordToMotion.EnablePreview || wordToMotion.IsPlayingBlendShape)
            {
                if (_hasAppliedEyebrowBlendShape)
                {
                    EyebrowBlendShape.UpdateEyebrowBlendShape(0, 0);
                    _hasAppliedEyebrowBlendShape = false;
                }

                return;
            }

            //NOTE: ここスケールファクタないと非常に小さい値しか入らないのでは？？？
            float left = _leftEyeBrowValue - _faceTracker.CalibrationData.eyeBrowPosition;
            float right = _rightEyeBrowValue - _faceTracker.CalibrationData.eyeBrowPosition;
            //顔トラッキングしない場合、つねに0が入るようにしとく
            if (!_faceTracker.HasInitDone ||
                !_faceTracker.FaceDetectedAtLeastOnce ||
                PreferAutoBlink)
            {
                left = 0;
                right = 0;
            }

            float goalLeft = left;
            float idealLeft = (goalLeft - _prevLeftEyeBrowWeight) / timeScaleFactor;
            float speedLeft = Mathf.Lerp(_prevLeftEyeBrowSpeed, idealLeft, speedLerpFactor);
            float weightLeft = _prevLeftEyeBrowWeight + Time.deltaTime * speedLeft;
            weightLeft = Mathf.Clamp(weightLeft, -1, 1);

            float goalRight = right;
            float idealRight = (goalRight - _prevRightEyeBrowWeight) / timeScaleFactor;
            float speedRight = Mathf.Lerp(_prevRightEyeBrowSpeed, idealRight, speedLerpFactor);
            float weightRight = _prevRightEyeBrowWeight + Time.deltaTime * speedRight;
            weightRight = Mathf.Clamp(weightRight, -1, 1);
            if (PreferAutoBlink)
            {
                speedLeft = 0;
                weightLeft = 0;
                speedRight = 0;
                weightRight = 0;
            }

            //まばたき量に応じた値も足す: こちらはまばたき側の計算時にすでにローパスされてるから、そのまま足してOK
            //weightToAssignのオフセット項は後付けの補正なので速度の計算基準に使わないよう、計算から外している
            float blinkLeft = _blendShapeProxy.GetValue(BlinkLKey);
            float weightLeftToAssign = weightLeft + defaultOffset - blinkLeft * eyeBrowDownOffsetWhenEyeClosed;

            float blinkRight = _blendShapeProxy.GetValue(BlinkRKey);
            float weightRightToAssign = weightRight + defaultOffset - blinkRight * eyeBrowDownOffsetWhenEyeClosed;

            EyebrowBlendShape.UpdateEyebrowBlendShape(weightLeftToAssign, weightRightToAssign);
            _hasAppliedEyebrowBlendShape = true;

            _prevLeftEyeBrowWeight = weightLeft;
            _prevLeftEyeBrowSpeed = speedLeft;
            _prevRightEyeBrowWeight = weightRight;
            _prevRightEyeBrowSpeed = speedRight;
        }

        private static bool CheckBlinkBlendShapeClips(VRMBlendShapeProxy proxy)
        {
            var avatar = proxy.BlendShapeAvatar;
            return (
                (avatar.GetClip(BlinkLKey).Values.Length > 0) &&
                (avatar.GetClip(BlinkRKey).Values.Length > 0) &&
                (avatar.GetClip(BlendShapePreset.Blink).Values.Length > 0)
            );
        }
    }
}
