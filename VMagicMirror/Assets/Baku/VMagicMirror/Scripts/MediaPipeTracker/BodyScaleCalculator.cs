using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    public class BodyScaleCalculator : PresenterBase
    {
        // 適当な標準体型での root-nose の高さ、および upperArm-hand の距離
        // ※noseまでの高さを使うのは、VRoid のモデルで大体鼻くらいの高さにheadボーンがあるため
        private const float DefaultBodyHeight = 1.5f;
        public const float DefaultArmLength = 0.6f;

        private readonly IVRMLoadable _vrmLoadable;
        
        
        // NOTE: 仮置きの値は「むちゃくちゃじゃなければOK」くらいのやつ
        public float LeftArmLength { get; private set; } = DefaultArmLength;
        public float RightArmLength { get; private set; } = DefaultArmLength;
        public float BodyHeight { get; private set; } = DefaultBodyHeight;
        
        // Tポーズの状態で取得した各ボーンの位置
        public Vector3 RootToHead { get; private set; }
        public Vector3 RootToLeftUpperArm { get; private set; }
        public Vector3 RootToRightUpperArm { get; private set; }

        public float BodyHeightFactor { get; private set; } = 1f;
        
        [Inject]
        public BodyScaleCalculator(IVRMLoadable vrmLoadable)
        {
            _vrmLoadable = vrmLoadable;
        }
        
        public override void Initialize()
        {
            _vrmLoadable.VrmLoaded += OnVrmLoaded;
        }

        // TODO: 体型計算してるコード、統一したい…
        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            var target = info.animator;
            
            var rootPosition = target.transform.position;

            var head = target.GetBoneTransform(HumanBodyBones.Head);
            var leftUpperArm = target.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            var leftWrist = target.GetBoneTransform(HumanBodyBones.LeftHand);
            
            var rightUpperArm = target.GetBoneTransform(HumanBodyBones.RightUpperArm);
            var rightWrist = target.GetBoneTransform(HumanBodyBones.RightHand);

            var bodyHeight = head.position.y - rootPosition.y;
            var leftArmLength = Vector3.Distance(leftWrist.position, leftUpperArm.position);
            var rightArmLength = Vector3.Distance(rightWrist.position, rightUpperArm.position);

            LeftArmLength = leftArmLength;
            RightArmLength = rightArmLength;
            BodyHeight = bodyHeight;
            BodyHeightFactor = bodyHeight / DefaultBodyHeight;

            RootToHead = head.position - rootPosition;
            RootToLeftUpperArm = leftUpperArm.position - rootPosition;
            RootToRightUpperArm = rightUpperArm.position - rootPosition;
        }

    }
}
