using System;
using Baku.VMagicMirror.MediaPipeTracker;
using UnityEngine;
using UniVRM10;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary> PCマイクまたは外部トラッキングのリップシンク情報をVRMに適用する、ちょっと偉いクラス。</summary>
    public class LipSyncIntegrator : MonoBehaviour
    {
        [SerializeField] private AnimMorphEasedTarget animMorphEasedTarget = null;
        [SerializeField] private ExternalTrackerLipSync externalTrackerLipSync = null;

        public bool PreferExternalTrackerLipSync { get; set; } = true;
        
        private FaceControlConfiguration _config;
        private MediaPipeLipSync _mediaPipeLipSync;

        private readonly float[] _valuesCache = new float[5];
        
        [Inject]
        public void Initialize(
            FaceControlConfiguration config,
            MediaPipeLipSync mediaPipeLipSync)
        {
            _config = config;
            _mediaPipeLipSync = mediaPipeLipSync;
        }

        public float VoiceRate
        {
            get
            {
                var src = GetCurrentLipSyncSource();
                _valuesCache[0] = src.A;
                _valuesCache[1] = src.I;
                _valuesCache[2] = src.U;
                _valuesCache[3] = src.E;
                _valuesCache[4] = src.O;
                return Mathf.Max(_valuesCache);
            }
        }
        
        public void Accumulate(ExpressionAccumulator accumulator, float weight = 1f)
        {
            var src = GetCurrentLipSyncSource();
            accumulator.Accumulate(ExpressionKey.Aa, src.A * weight);
            accumulator.Accumulate(ExpressionKey.Ih, src.I * weight);
            accumulator.Accumulate(ExpressionKey.Ou, src.U * weight);
            accumulator.Accumulate(ExpressionKey.Ee, src.E * weight);
            accumulator.Accumulate(ExpressionKey.Oh, src.O * weight);
        }

        private IMouthLipSyncSource GetCurrentLipSyncSource()
        {
            if (_config.HeadMotionControlMode.CurrentValue is FaceControlModes.ExternalTracker && PreferExternalTrackerLipSync)
            {
                return externalTrackerLipSync.LipSyncSource;
            }
            
            if (_config.HeadMotionControlMode.CurrentValue is FaceControlModes.WebCamHighPower && _mediaPipeLipSync.IsEnabledAndTracked)
            {
                return _mediaPipeLipSync.LipSyncSource;
            }

            //NOTE: マイクが無効な場合はanimMorphEasedTargetの出力がゼロになる、というのを想定した書き方にもなっている
            return animMorphEasedTarget.LipSyncSource;
        }
    }
}
