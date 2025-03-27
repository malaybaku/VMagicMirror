using UnityEngine;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    public class BodyScaleCalculator
    {
        // 適当な標準体型での root-nose の高さ、および upperArm-hand の距離
        // ※noseまでの高さを使うのは、VRoid のモデルで大体鼻くらいの高さにheadボーンがあるため
        private const float DefaultBodyHeight = 1.5f;
        public const float DefaultArmLength = 0.6f;

        // NOTE: 仮置きの値は「むちゃくちゃじゃなければOK」くらいのやつ
        public float ArmLength { get; private set; } = DefaultArmLength;
        public float BodyHeight { get; private set; } = DefaultBodyHeight;
        
        // Tポーズの状態で取得した各ボーンの位置
        public Vector3 RootToHead { get; private set; }
        public Vector3 RootToLeftUpperArm { get; private set; }
        public Vector3 RootToRightUpperArm { get; private set; }

        public float BodyHeightFactor { get; private set; } = 1f;

        /// <summary>
        /// 直立したAnimatorを渡すことで、体格に関する諸元を計算する
        /// </summary>
        /// <param name="target"></param>
        public void Calculate(Animator target)
        {
            var rootPosition = target.transform.position;

            var head = target.GetBoneTransform(HumanBodyBones.Head);
            var leftUpperArm = target.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            var leftWrist = target.GetBoneTransform(HumanBodyBones.LeftHand);
            
            var rightUpperArm = target.GetBoneTransform(HumanBodyBones.RightUpperArm);
            var rightWrist = target.GetBoneTransform(HumanBodyBones.RightHand);

            var bodyHeight = head.position.y - rootPosition.y;
            var armLength = Vector3.Distance(leftWrist.position, leftUpperArm.position);

            ArmLength = armLength;
            BodyHeight = bodyHeight;
            BodyHeightFactor = bodyHeight / DefaultBodyHeight;

            RootToHead = head.position - rootPosition;
            RootToLeftUpperArm = leftUpperArm.position - rootPosition;
            RootToRightUpperArm = rightUpperArm.position - rootPosition;
        }
    }
}
