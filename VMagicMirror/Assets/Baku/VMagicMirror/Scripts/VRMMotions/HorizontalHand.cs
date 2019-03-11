using UnityEngine;

namespace Baku.VMagicMirror
{
    //VRMの左右の手を常に水平にする
    public class HorizontalHand : MonoBehaviour
    {
        public Transform rightHand;
        public Transform leftHand;
        public Animator animator;

        private Quaternion RightHandOffsetRotation { get; } = Quaternion.AngleAxis(-90, Vector3.up);
        private Quaternion LeftHandOffsetRotation { get; } = Quaternion.AngleAxis(90, Vector3.up);

        void LateUpdate()
        {
            if (rightHand != null)
            {
                rightHand.rotation =
                    RightHandOffsetRotation * 
                    Quaternion.LookRotation(
                        new Vector3(rightHand.position.x, 0, rightHand.position.z),
                        Vector3.up
                        );
            }

            if (leftHand != null)
            {
                leftHand.rotation =
                    LeftHandOffsetRotation * 
                    Quaternion.LookRotation(
                        new Vector3(leftHand.position.x, 0, leftHand.position.z),
                        Vector3.up
                        );
            }
        }
    }
}
