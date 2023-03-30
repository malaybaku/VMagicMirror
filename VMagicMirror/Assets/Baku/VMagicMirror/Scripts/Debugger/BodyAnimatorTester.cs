using UnityEngine;

namespace Baku.VMagicMirror
{
    public class BodyAnimatorTester : MonoBehaviour
    {
        private static readonly int Active = Animator.StringToHash("Active");
        private static readonly int Jump = Animator.StringToHash("Jump");
        private static readonly int MoveRight = Animator.StringToHash("MoveRight");
        private static readonly int MoveForward = Animator.StringToHash("MoveForward");
        private static readonly int Crouch = Animator.StringToHash("Crouch");

        [SerializeField] private Animator animator;
        [SerializeField] private bool setAnimatorActive;
        [SerializeField] [Range(-1f, 1f)] private float moveRight;
        [SerializeField] [Range(-1f, 1f)] private float moveForward;
        [SerializeField] private bool jump;
        [SerializeField] private bool crouch;
        
        private bool _errorHappened;

        private void Start()
        {
            if (animator == null)
            {
                _errorHappened = true;
                Debug.LogError("Animator is missing: please assign valid animator");
            }
        }
        
        private void Update()
        {
            if (_errorHappened)
            {
                return;
            }

            animator.SetBool(Active, setAnimatorActive);
            animator.SetFloat(MoveRight, moveRight);
            animator.SetFloat(MoveForward, moveForward);
            animator.SetBool(Crouch, crouch);
            if (jump)
            {
                jump = false;
                animator.SetTrigger(Jump);
            }
        }
        
    }
}

