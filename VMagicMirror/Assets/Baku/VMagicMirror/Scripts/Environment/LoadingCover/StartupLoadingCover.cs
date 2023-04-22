using TMPro;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public class StartupLoadingCover : MonoBehaviour
    {
        private static readonly int FadeOut = Animator.StringToHash("FadeOut");
        private static readonly int HiddenStateHash = Animator.StringToHash("Hidden");

        [SerializeField] private Animator animator;
        [SerializeField] private TextMeshProUGUI loadingText;

        private bool _fadeOutCalled = false;

        public void FadeOutAndDestroySelf()
        {
            if (_fadeOutCalled)
            {
                return;
            }

            _fadeOutCalled = true;
            animator.SetTrigger(FadeOut);
        }

        private void Update()
        {
            if (animator.GetCurrentAnimatorStateInfo(0).shortNameHash == HiddenStateHash)
            {
                Destroy(gameObject);
            }
        }

        public void FadeOutImmediate()
        {
            Destroy(gameObject);
        }

        public void SetModelLoadIndication()
        {
            loadingText.text = "Loading Model...";
        }
    }
}
