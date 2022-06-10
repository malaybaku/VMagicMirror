using System.Linq;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    public class ColliderBasedAvatarParamLoader : MonoBehaviour
    {
        //手のオフセットについて、メッシュのレイキャストで計算するとピッタリすぎるので少し余裕を持つ
        private const float ConstOffset = 0.01f;
        private const float FallbackOffset = 0.02f;

        [SerializeField] private MeshCollider colliderPrefab;

        public float LeftHandOffsetY { get; private set; }
        public float RightHandOffsetY { get; private set; }

        public float MeanOffset => 0.5f * (LeftHandOffsetY + RightHandOffsetY);

        [Inject]
        public void Initialize(IVRMLoadable vrmLoadable)
        {
            vrmLoadable.VrmLoaded += info =>
            {
                ReadParams(info);
                Debug.Log($"Wrist to hand mean Offset = {MeanOffset:0.000}");
            };
        }

        private void ReadParams(VrmLoadedInfo info)
        {
            var leftHand = info.animator.GetBoneTransform(HumanBodyBones.LeftHand).position;
            var leftPalm = GetLeftHandRaycastReferencePosition(info.animator, leftHand);
            
            var rightHand = info.animator.GetBoneTransform(HumanBodyBones.RightHand).position;
            var rightPalm = GetRightHandRaycastReferencePosition(info.animator, rightHand);
            
            var height = info.animator.GetBoneTransform(HumanBodyBones.Head).position.y;

            using (var colliders = AvatarColliders.LoadMeshColliders(
                info.vrmRoot.gameObject, colliderPrefab, transform))
            {
                var results = new RaycastHit[colliders.Colliders.Length];
                
                //NOTE: Ray.originは高すぎるとデカい手を検出できず、低すぎるとスカートとかを誤検出するので、ほどほどに設定する
                var leftRay = new Ray(new Vector3(leftPalm.x, leftPalm.y - height * 0.3f, leftPalm.z), Vector3.up);
                var leftRayCount = Physics.RaycastNonAlloc(leftRay, results, leftHand.y - leftRay.origin.y);
                if (leftRayCount == 0)
                {
                    //(なぜか)メッシュが見つからない→フォールバック距離扱いにしておく
                    LeftHandOffsetY = FallbackOffset;
                }
                else
                {
                    LeftHandOffsetY = leftPalm.y - results.Take(leftRayCount).Max(hit => hit.point.y) + ConstOffset;
                }
                
                var rightRay = new Ray(new Vector3(rightPalm.x, rightPalm.y - height * 0.3f, rightPalm.z), Vector3.up);
                var rightRayCount = Physics.RaycastNonAlloc(rightRay, results, rightHand.y - rightRay.origin.y);
                if (rightRayCount == 0)
                {
                    //(なぜか)メッシュが見つからない→フォールバック距離扱いにしておく
                    RightHandOffsetY = FallbackOffset;
                }
                else
                {
                    RightHandOffsetY = rightPalm.y - results.Take(rightRayCount).Max(hit => hit.point.y) + ConstOffset;
                }
            }
        }

        //手首ボーンと中指付け根ボーンの中点を取得します。この点はレイキャストの基準として使われます。
        private Vector3 GetLeftHandRaycastReferencePosition(Animator animator, Vector3 leftHandPosition)
        {
            var finger = animator.GetBoneTransform(HumanBodyBones.LeftMiddleIntermediate);
            if (finger != null)
            {
                return finger.position;
            }
            
            finger = animator.GetBoneTransform(HumanBodyBones.LeftMiddleDistal);
            if (finger != null)
            {
                return finger.position;
            }

            finger = animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal);
            if (finger != null)
            {
                return finger.position;
            }
            
            //指ボーンがなければ諦める
            return leftHandPosition;
        }

        private Vector3 GetRightHandRaycastReferencePosition(Animator animator, Vector3 rightHandPosition)
        {
            var finger = animator.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate);
            if (finger != null)
            {
                return finger.position;
            }
            
            finger = animator.GetBoneTransform(HumanBodyBones.RightMiddleDistal);
            if (finger != null)
            {
                return finger.position;
            }

            finger = animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
            if (finger != null)
            {
                return finger.position;
            }
            
            //指ボーンがなければ諦める
            return rightHandPosition;
        }
    }
}