using UnityEngine;
using Zenject;
using VRM.Optimize;
using VRM.Optimize.Jobs;

namespace Baku.VMagicMirror
{
    [RequireComponent(typeof(CentralizedJobScheduler))]
    public class ReplacedVRMSpringBoneScheduler : MonoBehaviour
    {
        [Inject]
        public void Initialize(IVRMLoadable vrmLoadable)
        {
            _vrmLoadable = vrmLoadable;
        }
        
        private IVRMLoadable _vrmLoadable;
        private CentralizedJobScheduler _scheduler = null;
        private GameObject _vrm = null;

        private void Start()
        {
            _scheduler = GetComponent<CentralizedJobScheduler>();
            //NOTE: タイミングが早くないとダメ。VRMWindより先に初期化したい
            _vrmLoadable.PreVrmLoaded += OnVrmLoaded;
            _vrmLoadable.VrmDisposing += OnVrmUnloading;
        }

        private void OnVrmUnloading()
        {
            _scheduler.RemoveBuffer(_vrm);
            _vrm = null;
        }

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            _vrm = info.vrmRoot.gameObject;
            ReplaceComponents.ReplaceJobs(_vrm);
            _scheduler.AddBuffer(_vrm);
        }
    }
}
