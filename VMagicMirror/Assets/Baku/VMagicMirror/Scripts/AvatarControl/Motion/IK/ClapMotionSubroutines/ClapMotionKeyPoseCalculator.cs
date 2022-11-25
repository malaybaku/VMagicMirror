using Baku.VMagicMirror.IK.Utils;
using UnityEngine;
using UniVRM10;

namespace Baku.VMagicMirror.IK
{
    public class ClapMotionKeyPoseCalculator
    {
        //NOTE: 乱数の幅情報だけをSOで受けたい。カーブの情報とかはコードで書いちゃう。

        //パチパチの動作は完全に真横に手が動くわけではないので、少しだけ「離れてるときのほうが手の位置が高い」というようにする。
        //指先が真上方向を向く拍手の場合、0~ちょっとプラスの値でそれっぽくなる。
        private const float HeightOffset = 0.01f;
        //拍手のときの手首のスナップ。値が大きいと子供っぽい + 疲れそうな動きになるので、ほどほどに。
        private const float DistantAngleOffset = 12f;

        private const float ReferenceArmLength = SettingAutoAdjuster.ReferenceArmLength;

        private float _armLengthScale = 1f;
        
        public float HandOffset { get; set; } = 0.02f;

        public float ShortDistance => 0.03f * _armLengthScale;
        public float LongDistance => 0.06f * _armLengthScale;

        //NOTE: このスケールは軌道生成の最初に1回だけ乱数で更新され、同じ軌道を考える間では更新されない。
        //再生側では「小さい動きの場合、素早く再生する」みたいなことを考慮する
        public float MotionScale { get; set; } = 1f;

        //NOTE: モデルのロード前に事故らないよう、それっぽい値にしておく
        private float _clapHeight = 1f;
        private float _armLength = 0.5f;

        public void SetupAvatarBodyParameter(Animator animator)
        {
            //左右の腕長さがあまりに違うケースも無視、難しいので
            var leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            var leftLowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            var leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            var leftArmLength =
                Vector3.Distance(leftUpperArm.position, leftLowerArm.position) +
                Vector3.Distance(leftLowerArm.position, leftHand.position);

            var rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            var rightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            var rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            var rightArmLength =
                Vector3.Distance(rightUpperArm.position, rightLowerArm.position) +
                Vector3.Distance(rightLowerArm.position, rightHand.position);

            //chestは必須ボーンではないことに注意
            var chestBone = animator.GetBoneTransform(HumanBodyBones.Chest);
            if (chestBone == null)
            {
                chestBone = animator.GetBoneTransform(HumanBodyBones.Spine);
            }
            var chest = chestBone.position;
            var head = animator.GetBoneTransform(HumanBodyBones.Head).position;

            //ClapHeightは手首を持ってく位置を指定することに注意。気持ち低めを狙う。
            _clapHeight = Mathf.Lerp(chest.y, head.y, 0.4f);
            _armLength = 0.5f * (leftArmLength + rightArmLength);

            _armLengthScale = _armLength / ReferenceArmLength;
        }
        
        public HandPose GetClapBasePose()
        {
            //胸の正面で、腕は「肘が伸び切った状態から120度曲げた状態」くらいになる、という位置を見込むとこんな感じ
            var pos = new Vector3(0f, _clapHeight, _armLength * 0.5f);
            
            //TODO: ここに位置に関する乱数を挟んでよい。回転は無しでOK

            return new HandPose(pos, Quaternion.identity);
        }

        //拍手の中心位置を指定することで、拍手状態になっているときの手のIKポーズを計算する。
        public (HandPoses poses, float resultYaw) GetClapCenterPoses(HandPose basePose)
        {
            var rot = basePose.Rotation;

            //NOTE: pitch/yawは乱数でブレて欲しい
            var pitchBase = 30f;
            var pitchOffset = 10f;
            var yaw = 25f;

            var rightRot = rot * Quaternion.Euler(pitchBase - pitchOffset, yaw, 0f) * Quaternion.Euler(0f, 180f, 90f);
            var leftRot = rot * Quaternion.Euler(pitchBase + pitchOffset, yaw, 0f) * Quaternion.Euler(0f, 180f, -90f);

            var yawRot = Quaternion.Euler(0f, yaw, 0f);
            var rightPos = basePose.Position + yawRot * new Vector3(HandOffset, 0f, 0f);
            var leftPos = basePose.Position + yawRot * new Vector3(-HandOffset, 0f, 0f);

            var poses = new HandPoses(
                new HandPose(leftPos, leftRot),
                new HandPose(rightPos, rightRot)
            );
            return (poses, yaw);
        }

        //拍手の中心の姿勢情報を受け取って、手が離れた状態のポーズを計算する
        public HandPoses GetClapDistantPoses(HandPoses centerPoses, float yaw, float distance)
        {
            var yawRot = Quaternion.Euler(0f, yaw, 0f);
            //NOTE: 乱数でブレさせてよい + 固定でいい気もする
            var heightOffset = HeightOffset * MotionScale;
            var scaledDistance = distance * MotionScale;
            
            var leftPos = centerPoses.Left.Position +
                yawRot * new Vector3(-scaledDistance, 0f, 0f) +
                new Vector3(0f, heightOffset, 0f);

            var rightPos = centerPoses.Right.Position +
                yawRot * new Vector3(scaledDistance, 0f, 0f) + 
                new Vector3(0f, heightOffset, 0f);

            //NOTE: 0~の範囲で乱数でバラしてよい / 左右で違う手首のスナップが効くのはOK
            var leftAngleOffset = DistantAngleOffset;
            var rightAngleOffset = DistantAngleOffset;

            var leftRot = centerPoses.Left.Rotation * Quaternion.Euler(0f, 0f, -leftAngleOffset);
            var rightRot = centerPoses.Right.Rotation * Quaternion.Euler(0f, 0f, rightAngleOffset);

            return new HandPoses(new HandPose(leftPos, leftRot), new HandPose(rightPos, rightRot));
        }
    }
}
