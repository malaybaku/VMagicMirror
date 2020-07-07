using Baku.VMagicMirror.IK;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 全身の位置をずらす処理をやるクラス
    /// </summary>
    public class BodyMotionManager : MonoBehaviour
    {
        [SerializeField] private BodyLeanIntegrator bodyLeanIntegrator = null;
        [SerializeField] private ImageBasedBodyMotion imageBasedBodyMotion = null;
        [SerializeField] private ExternalTrackerBodyOffset exTrackerBodyMotion = null;
        [SerializeField] private WaitingBodyMotion waitingBodyMotion = null;

        public WaitingBodyMotion WaitingBodyMotion => waitingBodyMotion;

        private Transform _bodyIk = null;

        private Transform _vrmRoot = null;
        private Vector3 _defaultBodyIkPosition;
        private bool _isVrmLoaded = false;

        [Inject]
        public void Initialize(IVRMLoadable vrmLoadable, IKTargetTransforms ikTargets)
        {
            _bodyIk = ikTargets.Body;
            vrmLoadable.VrmLoaded += OnVrmLoaded;
            vrmLoadable.VrmDisposing += OnVrmDisposing;
        }
        
        private void Update()
        {
            if (!_isVrmLoaded)
            {
                return;
            }

            _bodyIk.localPosition =
                _defaultBodyIkPosition + 
                imageBasedBodyMotion.BodyIkXyOffset + 
                bodyLeanIntegrator.BodyOffsetSuggest + 　
                exTrackerBodyMotion.BodyOffset +
                waitingBodyMotion.Offset;

            //画像ベースの移動量はIKと体に利かす -> 体に移動量を足さないと腰だけ動いて見た目が怖くなります
            _vrmRoot.position = imageBasedBodyMotion.BodyIkXyOffset + exTrackerBodyMotion.BodyOffset;

            //スムージングはサブクラスの方でやっているのでコッチでは処理不要。
            _vrmRoot.localRotation = bodyLeanIntegrator.BodyLeanSuggest;
        }
        
        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            //NOTE: VRMLoadControllerがロード時点でbodyIkの位置をキャラのHipsあたりに調整しているので、それを貰う
            _defaultBodyIkPosition = _bodyIk.position;
            imageBasedBodyMotion.OnVrmLoaded(info);
            _vrmRoot = info.vrmRoot;

            _isVrmLoaded = true;
        }

        private void OnVrmDisposing()
        {
            _isVrmLoaded = false;

            _vrmRoot = null;
            imageBasedBodyMotion.OnVrmDisposing();
        }

        public void EnableImageBaseBodyLeanZ(bool enable)
            => imageBasedBodyMotion.EnableBodyLeanZ = enable;
    }
}
