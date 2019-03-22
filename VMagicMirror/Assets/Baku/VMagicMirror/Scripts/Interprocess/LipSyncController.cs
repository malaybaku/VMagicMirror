using UnityEngine;

namespace Baku.VMagicMirror
{
    public class LipSyncController : MonoBehaviour
    {
        private AnimMorphEasedTarget _animMorphTarget = null;

        private void Start()
        {
            _animMorphTarget = GetComponent<AnimMorphEasedTarget>();
        }

        public void SetLipSyncEnable(bool isEnabled)
        {
            _animMorphTarget.enabled = isEnabled;
        }
    }
}
