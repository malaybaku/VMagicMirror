using System;
using Baku.VMagicMirror.ExternalTracker;
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

        //ちょっとデフォルトで眉を上げとこう的な値。目の全開きは珍しいという仮説による。
        [SerializeField] private float defaultOffset = 0.2f;

        [SerializeField] private float eyeBrowDownOffsetWhenEyeClosed = 0.7f;

        [SerializeField] private float eyeAngleDegreeWhenEyeClosed = 10f;

        [Inject]
        public void Initialize(
            IVRMLoadable vrmLoadable, 
            FaceTracker faceTracker, 
            ExternalTrackerDataSource exTracker,
            FaceControlConfiguration config)
        {
            _config = config;
            _faceTracker = faceTracker;
            _exTracker = exTracker;
            vrmLoadable.VrmLoaded += OnVrmLoaded;
            vrmLoadable.VrmDisposing += OnVrmDisposing;
        }

        private FaceControlConfiguration _config;
        private FaceTracker _faceTracker = null;
        private ExternalTrackerDataSource _exTracker = null;

        private EyebrowBlendShapeSet EyebrowBlendShape => faceControlManager.EyebrowBlendShape;

        private VRMBlendShapeProxy _blendShapeProxy = null;
        private Transform _rightEyeBone = null;
        private Transform _leftEyeBone = null;

        //このクラス上でリセットされていないBlendShapeを送った状態かどうかのフラグ。
        private bool _hasAppliedEyebrowBlendShape = false;

        private IDisposable _rightEyeBrowHeight = null;
        private IDisposable _leftEyeBrowHeight = null;

        //「目ボーンがある + まばたきブレンドシェイプがある」の2つで判定
        private bool _hasValidEyeSettings = false;

        public bool IsInitialized { get; private set; } = false;
        
        private void LateUpdate()
        {
            //モデルロードの前
            if (!IsInitialized)
            {
                return;
            }

            AdjustEyeRotation();

            //パーフェクトシンクが動いてるとき、眉は眉単体で動かせるため、このスクリプトの仕事はない
            if (_exTracker.Connected && _config.ShouldStopEyeDownOnBlink)
            {
                //自前で動かしたぶんは直しておく:
                //  パーフェクトシンクの最初のLateUpdateが走る前にここを通すと破綻を防げるはず
                if (_hasAppliedEyebrowBlendShape)
                {
                    EyebrowBlendShape.UpdateEyebrowBlendShape(0, 0);
                    _hasAppliedEyebrowBlendShape = false;
                }
            }
            else
            {
                AdjustEyebrow();                
            }
        }

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            _blendShapeProxy = info.blendShape;
            _rightEyeBone = info.animator.GetBoneTransform(HumanBodyBones.RightEye);
            _leftEyeBone = info.animator.GetBoneTransform(HumanBodyBones.LeftEye);

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
            if (!_hasValidEyeSettings || _config.ShouldSkipNonMouthBlendShape)
            {
                return;
            }

            bool shouldUseAlternativeBlink = _exTracker.Connected && _config.ShouldStopEyeDownOnBlink;
            
            float leftBlink = shouldUseAlternativeBlink
                ? _config.AlternativeBlinkL
                : _blendShapeProxy.GetValue(BlinkLKey);
            float rightBlink = shouldUseAlternativeBlink
                ? _config.AlternativeBlinkR
                : _blendShapeProxy.GetValue(BlinkRKey);

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
            //if (wordToMotion.EnablePreview || wordToMotion.IsPlayingBlendShape)
            if (!_config.ShouldSkipNonMouthBlendShape)
            {
                if (_hasAppliedEyebrowBlendShape)
                {
                    EyebrowBlendShape.UpdateEyebrowBlendShape(0, 0);
                    _hasAppliedEyebrowBlendShape = false;
                }

                return;
            }

            //NOTE: ここもともとは顔トラッキングで入れた値が入ってたんだけど、まあ要らんだろ…と思って書き換えた、という経緯でこのような実装です。
            //まばたき量に応じた値を足していく: こちらはまばたき側の計算時にすでにローパスされてるから、そのまま足してOK
            //weightToAssignのオフセット項は後付けの補正なので速度の計算基準に使わないよう、計算から外している
            float blinkLeft = _blendShapeProxy.GetValue(BlinkLKey);
            float weightLeftToAssign = defaultOffset - blinkLeft * eyeBrowDownOffsetWhenEyeClosed;

            float blinkRight = _blendShapeProxy.GetValue(BlinkRKey);
            float weightRightToAssign = defaultOffset - blinkRight * eyeBrowDownOffsetWhenEyeClosed;

            EyebrowBlendShape.UpdateEyebrowBlendShape(weightLeftToAssign, weightRightToAssign);
            _hasAppliedEyebrowBlendShape = true;
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
