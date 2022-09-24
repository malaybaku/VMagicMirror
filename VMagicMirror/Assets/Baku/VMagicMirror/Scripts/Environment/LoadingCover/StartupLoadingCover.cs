using UnityEngine;

namespace Baku.VMagicMirror
{
    public class StartupLoadingCover : MonoBehaviour
    {
        private const float DestroySelfDelay = 1.0f;
        private static readonly int Unfade = Animator.StringToHash("Unfade");

        [SerializeField] private Animator animator;

        private bool _unfadeCalled = false;

        public void UnfadeAndDestroySelf()
        {
            if (_unfadeCalled)
            {
                return;
            }

            _unfadeCalled = true;
            animator.SetTrigger(Unfade);
            Invoke(nameof(DestroySelf), DestroySelfDelay);
        }

        private void DestroySelf()
        {
            Destroy(gameObject);
        }
    }
}
