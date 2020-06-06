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
        private bool _hasModel = false;
        private VRMBlendShapeProxy _blendShape = null;

        [Inject]
        public void Initialize(IVRMLoadable vrmLoadable, FaceControlConfiguration config)
        {
            _config = config;
            
            vrmLoadable.VrmLoaded += info =>
            {
                _blendShape = info.blendShape;
                _hasModel = true;
            };
            
            vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _blendShape = null;
            };
        }
        
        private void Update()
        {
            if (!_hasModel || _config.ShouldSkipMouthBlendShape)
            {
                return;
            }
        
            //NOTE: マイクが無効な場合はanimMorphEasedTargetの出力がゼロになる、というのを想定した書き方でもあります
            var src = PreferExternalTrackerLipSync
                ? externalTrackerLipSync.LipSyncSource
                : animMorphEasedTarget.LipSyncSource;

            _blendShape.AccumulateValue(_a, src.A);
            _blendShape.AccumulateValue(_i, src.I);
            _blendShape.AccumulateValue(_u, src.U);
            _blendShape.AccumulateValue(_e, src.E);
            _blendShape.AccumulateValue(_o, src.O);
        }
    }
}
