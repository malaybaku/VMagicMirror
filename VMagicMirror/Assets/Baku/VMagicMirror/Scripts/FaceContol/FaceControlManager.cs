using UniGLTF;
using UnityEngine;
using VRM;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 表情制御の一番か二番目くらいに偉いやつ。VRMBlendShapeProxy.Applyをする権利を保有する。
    /// </summary>
    public class FaceControlManager : MonoBehaviour
    {
        [SerializeField] private FaceDetector faceDetector = null;
        
        //ブレンドシェイプ生成するやつ各位
        //TODO: MonoBehaviour継承を外すと改善という事になります。大体は。
        [SerializeField] private EyeDownBlendShapeController eyeDownController = null;
        [SerializeField] private AnimMorphEasedTarget animMorphEasedTarget = null;
        [SerializeField] private ImageBasedBlinkController blinkController = null;
        [SerializeField] private VRMAutoBlink autoBlink = null;
        //TODO: この辺にWord To Motionベースの何かが入るんじゃないかな？

        public VRMBlendShapeStore BlendShapeStore { get; } = new VRMBlendShapeStore();
        public EyebrowBlendShapeSet EyebrowBlendShape { get; } = new EyebrowBlendShapeSet();

        private VRMBlendShapeProxy _proxy = null;

        
        public DefaultFunBlendShapeModifier DefaultBlendShape { get; } 
            = new DefaultFunBlendShapeModifier();
        
        public void OnVrmLoaded(VrmLoadedInfo info)
        {
            _proxy = info.blendShape;
            eyeDownController.OnVrmLoaded(info);
            animMorphEasedTarget.OnVrmLoaded(info);
            blinkController.OnVrmLoaded(info);
            
            BlendShapeStore.OnVrmLoaded(info);
            EyebrowBlendShape.RefreshTarget(BlendShapeStore);
        }

        public void OnVrmDisposing()
        {
            _proxy = null;
            eyeDownController.OnVrmDisposing();
            animMorphEasedTarget.OnVrmDisposing();
            blinkController.OnVrmDisposing();
            
            BlendShapeStore.OnVrmDisposing();
            EyebrowBlendShape.Reset();
        }

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
            }
        }
        
        private void Update()
        {
        
        }
    }

}
