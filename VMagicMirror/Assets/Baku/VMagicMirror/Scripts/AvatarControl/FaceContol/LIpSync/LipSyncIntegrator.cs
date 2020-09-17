using UnityEngine;
using Zenject;
using VRM;

namespace Baku.VMagicMirror
{
    /// <summary> PCマイクまたは外部トラッキングのリップシンク情報をVRMに適用する、ちょっと偉いクラス。</summary>
    public class LipSyncIntegrator : MonoBehaviour
    {     
        private static readonly BlendShapeKey _a = new BlendShapeKey(BlendShapePreset.A);
        private static readonly BlendShapeKey _i = new BlendShapeKey(BlendShapePreset.I);
        private static readonly BlendShapeKey _u = new BlendShapeKey(BlendShapePreset.U);
        private static readonly BlendShapeKey _e = new BlendShapeKey(BlendShapePreset.E);
        private static readonly BlendShapeKey _o = new BlendShapeKey(BlendShapePreset.O);
        
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
        
        public void Accumulate(VRMBlendShapeProxy proxy)
        {
            //NOTE: マイクが無効な場合はanimMorphEasedTargetの出力がゼロになる、というのを想定した書き方でもあります
            var src = PreferExternalTrackerLipSync
                ? externalTrackerLipSync.LipSyncSource
                : animMorphEasedTarget.LipSyncSource;

            proxy.AccumulateValue(_a, src.A);
            proxy.AccumulateValue(_i, src.I);
            proxy.AccumulateValue(_u, src.U);
            proxy.AccumulateValue(_e, src.E);
            proxy.AccumulateValue(_o, src.O);
        }
    }
}
