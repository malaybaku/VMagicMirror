using UnityEngine;

namespace Baku.VMagicMirror.IK
{
    /// <summary>
    /// IKのターゲットを取得する処理です。
    /// 使っていいクラスは非常に限られる(最終的に適用する人くらいである)ことに注意してください。
    /// </summary>
    public class IKTargets : MonoBehaviour
    {
        [SerializeField] private Transform lookAt = default;
        [SerializeField] private Transform rightHand = default;
        [SerializeField] private Transform leftHand = default;
        [SerializeField] private Transform rightIndex = default;
        [SerializeField] private Transform body = default;

        public Transform LookAt => lookAt;
        public Transform RightHand => rightHand;
        public Transform LeftHand => leftHand;
        public Transform RightIndex => rightIndex;
        public Transform Body => body;
    }
}
