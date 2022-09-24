using UnityEngine;

namespace Baku.VMagicMirror
{
    public class StartupLoadingCover : MonoBehaviour
    {
        private const float DestroySelfDelay = 1.2f;
        private static readonly int FadeOut = Animator.StringToHash("FadeOut");

        [SerializeField] private Animator animator;

        private bool _fadeOutCalled = false;

        public void FadeOutAndDestroySelf()
        {
            if (_fadeOutCalled)
            {
                return;
            }

            _fadeOutCalled = true;
            animator.SetTrigger(FadeOut);
            Invoke(nameof(DestroySelf), DestroySelfDelay);
        }

        private void DestroySelf()
        {
            Destroy(gameObject);
        }
    }
}
