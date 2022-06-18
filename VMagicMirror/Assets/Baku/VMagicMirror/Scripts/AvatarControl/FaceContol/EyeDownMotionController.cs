using Baku.VMagicMirror.ExternalTracker;
using UnityEngine;
using VRM;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary> 瞬きに対して目と眉を下げる処理をするやつ </summary>
    public class EyeDownMotionController : MonoBehaviour, IEyeRotationRequestSource
    {
        private static readonly BlendShapeKey BlinkLKey = BlendShapeKey.CreateFromPreset(BlendShapePreset.Blink_L);
        private static readonly BlendShapeKey BlinkRKey = BlendShapeKey.CreateFromPreset(BlendShapePreset.Blink_R);

        [SerializeField] private float eyeAngleRateWhenEyeClosed = 0.3f;

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
        private bool _hasValidEyeSettings = false;

        public bool IsInitialized { get; private set; } = false;

        bool IEyeRotationRequestSource.IsActive => _hasValidEyeSettings && IsInitialized;
        public Vector2 LeftEyeRotationRate { get; private set; }
        public Vector2 RightEyeRotationRate { get; private set; }
        
        private void LateUpdate()
        {
            //モデルロードの前
            if (!IsInitialized)
            {
                LeftEyeRotationRate = Vector2.zero;
                RightEyeRotationRate = Vector2.zero;
                return;
            }

            AdjustEyeRotation();
        }

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            _blendShapeProxy = info.blendShape;
            _hasValidEyeSettings = CheckBlinkBlendShapeClips(_blendShapeProxy);
            IsInitialized = true;
        }

        private void OnVrmDisposing()
        {
            IsInitialized = false;
            _hasValidEyeSettings = false;
            _blendShapeProxy = null;
        }

        private void AdjustEyeRotation()
        {
            //NOTE: どっちかというとWordToMotion用に"Disable/Enable"系のAPI出す方がいいかも
            if (!_hasValidEyeSettings || _config.ShouldSkipNonMouthBlendShape)
            {
                return;
            }

            var shouldUseAlternativeBlink = _exTracker.Connected && _config.ShouldStopEyeDownOnBlink;            

            // このへんの値が1フレーム前の値ではなく同一フレームの値を参照できるともっと良い
            var leftBlink = shouldUseAlternativeBlink
                ? _config.AlternativeBlinkL
                : _blendShapeProxy.GetValue(BlinkLKey);
            var rightBlink = shouldUseAlternativeBlink
                ? _config.AlternativeBlinkR
                : _blendShapeProxy.GetValue(BlinkRKey);

            LeftEyeRotationRate = new Vector2(0f, -leftBlink * eyeAngleRateWhenEyeClosed);
            RightEyeRotationRate = new Vector2(0f, -rightBlink * eyeAngleRateWhenEyeClosed);
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
