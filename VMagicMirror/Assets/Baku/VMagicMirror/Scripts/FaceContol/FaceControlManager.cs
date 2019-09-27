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
        [SerializeField] private FaceTracker faceTracker = null;
        
        //ブレンドシェイプ生成するやつ各位
        //TODO: MonoBehaviour継承を外すと改善という事になります。大体は。
        [SerializeField] private EyeDownBlendShapeController eyeDownController = null;
        [SerializeField] private AnimMorphEasedTarget animMorphEasedTarget = null;
        [SerializeField] private ImageBasedBlinkController imageBasedBlinkController = null;
        [SerializeField] private VRMAutoBlink autoBlink = null;

        [Inject] private IVRMLoadable _vrmLoadable = null;

        /// <summary>
        /// VRMロード時の初期化が済んだら発火
        /// </summary>
        public event Action VrmInitialized;

        public VRMBlendShapeStore BlendShapeStore { get; } = new VRMBlendShapeStore();
        public EyebrowBlendShapeSet EyebrowBlendShape { get; } = new EyebrowBlendShapeSet();

        private VRMBlendShapeProxy _proxy;

        private bool _preferAutoBlink = false;

        /// <summary> 顔トラッキング中であっても自動まばたきを優先するかどうか </summary>
        public bool PreferAutoBlink
        {
            get => _preferAutoBlink;
            set
            {
                _preferAutoBlink = value;
                eyeDownController.PreferAutoBlink = value;
            }
        } 
        
        public DefaultFunBlendShapeModifier DefaultBlendShape { get; } 
            = new DefaultFunBlendShapeModifier();
        
        private bool _overrideByMotion = false;
        
        public bool OverrideByMotion
        {
            get => _overrideByMotion;
            set
            {
                if (_overrideByMotion == value)
                {
                    return;
                }

                _overrideByMotion = value;
                if (value)
                {
                    DefaultBlendShape.Reset(_proxy);
                    autoBlink.Reset(_proxy);
                    //なんかリセット系のやつあれば他にも呼ぶ
                    
                }
                else
                {
                    //DefaultBlendShapeの適用とかしないとダメ?毎フレーム評価するなら不要だが
                }
            }
        }

        private void Start()
        {
            _vrmLoadable.VrmLoaded += OnVrmLoaded;
            _vrmLoadable.VrmDisposing += OnVrmDisposing;
        }
        
        private void Update()
        {
            if (_proxy == null)
            {
                return;
            }

            if (!OverrideByMotion)
            {
                DefaultBlendShape.Apply(_proxy);
                
                if (!PreferAutoBlink && faceTracker.FaceDetectedAtLeastOnce)
                {
                    imageBasedBlinkController.Apply(_proxy);
                }
                else
                {
                    autoBlink.Apply(_proxy);
                }
            }
        }
                
        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            _proxy = info.blendShape;
            BlendShapeStore.OnVrmLoaded(info);
            EyebrowBlendShape.RefreshTarget(BlendShapeStore);

            VrmInitialized?.Invoke();
        }

        private void OnVrmDisposing()
        {
            _proxy = null;
            BlendShapeStore.OnVrmDisposing();
            EyebrowBlendShape.Reset();
        }
    }
}
