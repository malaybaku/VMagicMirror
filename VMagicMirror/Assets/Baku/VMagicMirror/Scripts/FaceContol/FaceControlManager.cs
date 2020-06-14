using System;
using UnityEngine;
using VRM;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 表情制御の一番か二番目くらいに偉いやつ。VRMBlendShapeProxy.Applyをする権利を保有する。
    /// </summary>
    public class FaceControlManager : MonoBehaviour
    {
        private static readonly BlendShapeKey BlinkLKey = new BlendShapeKey(BlendShapePreset.Blink_L);
        private static readonly BlendShapeKey BlinkRKey = new BlendShapeKey(BlendShapePreset.Blink_R);

        //NOTE: まばたき自体は3種類どれかが排他で適用される。複数走っている場合、external > image > autoの優先度で適用する。
        [SerializeField] private ExternalTrackerBlink externalTrackerBlink = null;
        [SerializeField] private ImageBasedBlinkController imageBasedBlinkController = null;
        [SerializeField] private VRMAutoBlink autoBlink = null;

        //TODO: コイツの有効/無効をこのマネージャクラスで制御すべきか、というのが悩みどころ(止めて良い気もするし動かしっぱなしでいい気もする)
        [SerializeField] private EyeDownBlendShapeController eyeDownController = null;

        [SerializeField] private EyeJitter randomEyeJitter = null;
        [SerializeField] private ExternalTrackerEyeJitter externalTrackEyeJitter = null;
        
        private bool _hasModel = false;
        private VRMBlendShapeProxy _proxy;
        private FaceControlConfiguration _config;

        [Inject]
        public void Initialize(IVRMLoadable vrmLoadable, FaceControlConfiguration config)
        {
            _config = config;
            vrmLoadable.VrmLoaded += OnVrmLoaded;
            vrmLoadable.VrmDisposing += OnVrmDisposing;
        }
        
        /// <summary> VRMロード時の初期化が済んだら発火 </summary>
        public event Action VrmInitialized;

        public VRMBlendShapeStore BlendShapeStore { get; } = new VRMBlendShapeStore();
        public EyebrowBlendShapeSet EyebrowBlendShape { get; } = new EyebrowBlendShapeSet();

        public DefaultFunBlendShapeModifier DefaultBlendShape { get; } 
            = new DefaultFunBlendShapeModifier();

        /// <summary> WebCamベースのトラッキング中でも自動まばたきを優先するかどうかを取得、設定します。 </summary>
        public bool PreferAutoBlinkOnWebCamTracking { get; set; } = true;

        private void Update()
        {
            //眼球運動もモード別で切り替えていく
            bool canUseExternalEyeJitter =
                _config.ControlMode == FaceControlModes.ExternalTracker && externalTrackEyeJitter.IsTracked;
            randomEyeJitter.IsActive = !canUseExternalEyeJitter;
            externalTrackEyeJitter.IsActive = canUseExternalEyeJitter;

            if (!_hasModel)
            {
                return;
            }
            
            if (_config.ShouldSkipNonMouthBlendShape)
            {
                //TODO: これ系の「非ゼロにしたいBlendShapeを明示的に切る」処理をどこに入れるか、というのは悩みどころ
                //ResetBlink();
                return;
            }

            DefaultBlendShape.Apply(_proxy);
            
            var blinkSource =
                _config.ControlMode == FaceControlModes.ExternalTracker ? externalTrackerBlink.BlinkSource :
                (_config.ControlMode == FaceControlModes.WebCam && !PreferAutoBlinkOnWebCamTracking) ? imageBasedBlinkController.BlinkSource :
                autoBlink.BlinkSource;
            
            _proxy.AccumulateValue(BlinkLKey, blinkSource.Left);
            _proxy.AccumulateValue(BlinkRKey, blinkSource.Right);
        }
                
        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            _proxy = info.blendShape;
            BlendShapeStore.OnVrmLoaded(info);
            EyebrowBlendShape.RefreshTarget(BlendShapeStore);
            VrmInitialized?.Invoke();
            _hasModel = true;
        }

        private void OnVrmDisposing()
        {
            _hasModel = false;
            _proxy = null;
            BlendShapeStore.OnVrmDisposing();
            EyebrowBlendShape.Reset();
        }

        private void ResetBlink()
        {
            if (_hasModel)
            {
                _proxy.AccumulateValue(BlinkLKey, 0);
                _proxy.AccumulateValue(BlinkRKey, 0);
            }
        }
    }
    
    
    /// <summary> まばたき状態の値を提供します。 </summary>
    public interface IBlinkSource
    {
        float Left { get; }
        float Right { get; }
    }

    /// <summary> 単なるプロパティで<see cref="IBlinkSource"/>を実装します。 </summary>
    public class RecordBlinkSource : IBlinkSource
    {
        public float Left { get; set; }
        public float Right { get; set; }
    }
}
