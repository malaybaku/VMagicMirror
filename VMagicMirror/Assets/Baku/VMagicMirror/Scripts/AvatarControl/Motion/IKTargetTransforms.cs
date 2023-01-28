using UnityEngine;

namespace Baku.VMagicMirror.IK
{
    /// <summary>
    /// IKのターゲットを取得する処理です。
    /// 使っていいクラスは非常に限られる(最終的に適用する人くらいである)ことに注意してください。
    /// </summary>
    public class IKTargetTransforms : MonoBehaviour
    {
        [SerializeField] private Transform lookAt;
        [SerializeField] private Transform rightHand;
        [SerializeField] private Transform leftHand;
        [SerializeField] private Transform rightIndex;
        [SerializeField] private Transform body;
        [SerializeField] private Transform leftFoot;
        [SerializeField] private Transform rightFoot;
        [SerializeField] private CustomizableHandIkTarget leftHandDown;
        [SerializeField] private CustomizableHandIkTarget rightHandDown;

        public Transform LookAt => lookAt;
        public Transform RightHand => rightHand;
        public Transform LeftHand => leftHand;
        public Transform RightIndex => rightIndex;
        public Transform Body => body;
        public Transform LeftFoot => leftFoot;
        public Transform RightFoot => rightFoot;

        public CustomizableHandIkTarget LeftHandDown => leftHandDown;
        public CustomizableHandIkTarget RightHandDown => rightHandDown;
    }
}
