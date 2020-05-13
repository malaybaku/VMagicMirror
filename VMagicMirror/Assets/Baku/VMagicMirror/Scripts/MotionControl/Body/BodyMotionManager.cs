using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 全身の位置をずらす処理をやるクラス
    /// </summary>
    public class BodyMotionManager : MonoBehaviour
    {
        [SerializeField] private Transform bodyIk = null;

        [SerializeField] private BodyLeanIntegrator bodyLeanIntegrator = null;
        [SerializeField] private ImageBasedBodyMotion imageBasedBodyMotion = null;
        [SerializeField] private WaitingBodyMotion waitingBodyMotion = null;
        [Inject] private IVRMLoadable _vrmLoadable = null;

        public WaitingBodyMotion WaitingBodyMotion => waitingBodyMotion;

        private Transform _vrmRoot = null;
        private Vector3 _defaultBodyIkPosition;
        private bool _isVrmLoaded = false;
        
        private void Start()
        {
            _vrmLoadable.VrmLoaded += OnVrmLoaded;
            _vrmLoadable.VrmDisposing += OnVrmDisposing;
        }

        private void Update()
        {
            if (!_isVrmLoaded)
            {
                return;
            }

            bodyIk.localPosition =
                _defaultBodyIkPosition + 
                imageBasedBodyMotion.BodyIkXyOffset + 
                waitingBodyMotion.Offset;

            //画像ベースの移動量はIKと体に利かす -> 体に移動量を足さないと腰だけ動いて見た目が怖くなります
            _vrmRoot.position = imageBasedBodyMotion.BodyIkXyOffset;

            //スムージングはサブクラスの方でやっているのでコッチでは処理不要。
            _vrmRoot.localRotation = bodyLeanIntegrator.BodyLeanSuggest;
        }
        
        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            //NOTE: VRMLoadControllerがロード時点でbodyIkの位置をキャラのHipsあたりに調整しているので、それを貰う
            _defaultBodyIkPosition = bodyIk.position;
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
