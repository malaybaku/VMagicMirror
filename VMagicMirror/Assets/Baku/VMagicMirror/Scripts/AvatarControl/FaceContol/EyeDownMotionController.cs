using Baku.VMagicMirror.ExternalTracker;
using UnityEngine;
using VRM;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary> 瞬きに対して目と眉を下げる処理をするやつ </summary>
    public class EyeDownMotionController : MonoBehaviour
    {
        private static readonly BlendShapeKey BlinkLKey = new BlendShapeKey(BlendShapePreset.Blink_L);
        private static readonly BlendShapeKey BlinkRKey = new BlendShapeKey(BlendShapePreset.Blink_R);

        [SerializeField] private float eyeAngleDegreeWhenEyeClosed = 10f;

        [Inject]
        public void Initialize(
            IVRMLoadable vrmLoadable, 
            ExternalTrackerDataSource exTracker,
            FaceControlConfiguration config)
        {
            _config = config;
            _exTracker = exTracker;
            vrmLoadable.VrmLoaded += OnVrmLoaded;
            vrmLoadable.VrmDisposing += OnVrmDisposing;
        }

        private FaceControlConfiguration _config;
        private ExternalTrackerDataSource _exTracker = null;
        
        private VRMBlendShapeProxy _blendShapeProxy = null;
        private Transform _rightEyeBone = null;
        private Transform _leftEyeBone = null;

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
