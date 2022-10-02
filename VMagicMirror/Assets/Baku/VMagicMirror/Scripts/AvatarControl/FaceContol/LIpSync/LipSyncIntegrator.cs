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

        public bool PreferExternalTrackerLipSync { get; set; } = false;
        
        private FaceControlConfiguration _config;

        [Inject]
        public void Initialize(FaceControlConfiguration config)
        {
            _config = config;
        }

        public float VoiceRate
        {
            get
            {
                var src = PreferExternalTrackerLipSync
                    ? externalTrackerLipSync.LipSyncSource
                    : animMorphEasedTarget.LipSyncSource;
                //NOTE: params引数を使うと配列化されそうでヤダなあという書き方です
                return Mathf.Max(src.A, Mathf.Max(src.I, Mathf.Max(src.U, Mathf.Max(src.E, src.O))));
            }
        }
        
        public void Accumulate(ExpressionAccumulator accumulator, float weight = 1f)
        {
            //NOTE: マイクが無効な場合はanimMorphEasedTargetの出力がゼロになる、というのを想定した書き方でもあります
            var src = PreferExternalTrackerLipSync
                ? externalTrackerLipSync.LipSyncSource
                : animMorphEasedTarget.LipSyncSource;

            accumulator.Accumulate(ExpressionKey.Aa, src.A * weight);
            accumulator.Accumulate(ExpressionKey.Ih, src.I * weight);
            accumulator.Accumulate(ExpressionKey.Ou, src.U * weight);
            accumulator.Accumulate(ExpressionKey.Ee, src.E * weight);
            accumulator.Accumulate(ExpressionKey.Oh, src.O * weight);
        }
    }
}
