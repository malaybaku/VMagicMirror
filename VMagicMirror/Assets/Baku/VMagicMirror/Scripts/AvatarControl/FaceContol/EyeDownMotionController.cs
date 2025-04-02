using Baku.VMagicMirror.ExternalTracker;
using UnityEngine;
using UniVRM10;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary> 瞬きに対して目を下げる処理をするやつ </summary>
    public class EyeDownMotionController : MonoBehaviour, IEyeRotationRequestSource
    {
        [SerializeField] private float eyeDownRateWhenEyeClosed = 1.0f;

        [Inject]
        public void Initialize(
            IVRMLoadable vrmLoadable, 
            ExternalTrackerDataSource exTracker,
            FaceControlConfiguration config,
            ExpressionAccumulator resultSetter)
        {
            _config = config;
            _exTracker = exTracker;
            _accumulator = resultSetter;
            vrmLoadable.VrmLoaded += OnVrmLoaded;
            vrmLoadable.VrmDisposing += OnVrmDisposing;
        }

        private FaceControlConfiguration _config;
        private ExternalTrackerDataSource _exTracker = null;

        private Vrm10RuntimeExpression _runtimeExpression = null;
        private bool _hasValidEyeSettings = false;

        private ExpressionAccumulator _accumulator = null;
        
        bool IEyeRotationRequestSource.IsActive => _hasValidEyeSettings && !_config.ShouldSkipNonMouthBlendShape;
        public Vector2 LeftEyeRotationRate { get; private set; }
        public Vector2 RightEyeRotationRate { get; private set; }

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            _runtimeExpression = info.RuntimeFacialExpression;
            _hasValidEyeSettings = CheckBlinkBlendShapeClips(info.instance.Vrm.Expression);
        }

        private void OnVrmDisposing()
        {
            _hasValidEyeSettings = false;
            _runtimeExpression = null;
        }

        public void UpdateRotationRate()
        {
            //NOTE: どっちかというとWordToMotion用に"Disable/Enable"系のAPI出す方がいいかも
            if (!_hasValidEyeSettings || _config.ShouldSkipNonMouthBlendShape)
            {
                LeftEyeRotationRate = Vector2.zero;
                RightEyeRotationRate = Vector2.zero;
                return;
            }

            var shouldUseAlternativeBlink = _exTracker.Connected && _config.ShouldStopEyeDownOnBlink;            

            // このへんの値が1フレーム前の値ではなく同一フレームの値を参照できるともっと良い
            var leftBlink = shouldUseAlternativeBlink
                ? _config.AlternativeBlinkL
                : _accumulator.GetValue(ExpressionKey.BlinkLeft);
            //_runtimeExpression.GetWeight(ExpressionKey.BlinkLeft);
            var rightBlink = shouldUseAlternativeBlink
                ? _config.AlternativeBlinkR
                : _accumulator.GetValue(ExpressionKey.BlinkRight);
            // _runtimeExpression.GetWeight(ExpressionKey.BlinkRight);

            LeftEyeRotationRate = new Vector2(0f, -leftBlink * eyeDownRateWhenEyeClosed);
            RightEyeRotationRate = new Vector2(0f, -rightBlink * eyeDownRateWhenEyeClosed);
        }

        private static bool CheckBlinkBlendShapeClips(VRM10ObjectExpression settings)
        {
            //NOTE: 隻眼/単眼でも補正はかかってほしい、という事でこういう感じ
            return (settings.BlinkLeft.HasValidBinds() || settings.BlinkRight.HasValidBinds()) &&
                settings.Blink.HasValidBinds();
        }
    }
}
