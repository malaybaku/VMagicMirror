using AniLipSync.VRM;
using UnityEngine;

namespace Baku.VMagicMirror
{
    [RequireComponent(typeof(AnimMorphTarget))]
    public class LipSyncController : MonoBehaviour
    {
        private AnimMorphTarget _animMorphTarget = null;

        private void Start()
        {
            _animMorphTarget = GetComponent<AnimMorphTarget>();
        }

        public void SetLipSyncEnable(bool isEnabled)
        {
            _animMorphTarget.enabled = isEnabled;
        }
    }
}
