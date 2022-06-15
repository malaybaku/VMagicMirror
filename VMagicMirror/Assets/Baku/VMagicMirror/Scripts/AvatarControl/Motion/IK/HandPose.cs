using UnityEngine;

namespace Baku.VMagicMirror.IK.Utils
{
    public readonly struct HandPoses
    {
        public HandPoses(HandPose left, HandPose right)
        {
            Left = left;
            Right = right;
        }

        public HandPose Left { get; }
        public HandPose Right { get; }

        public static HandPoses Lerp(HandPoses from, HandPoses to, float t)
        {
            return new HandPoses(
                HandPose.Lerp(from.Left, to.Left, t),
                HandPose.Lerp(from.Right, to.Right, t)
            );
        }
    }
    
    //unityのPoseとだいたい同じやつ
    public readonly struct HandPose
    {
        public HandPose(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }
        
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }

        public Vector3 GetPosition(Vector3 localPosition) => Position + Rotation * localPosition;

        public static HandPose FromPosition(Vector3 position) => new HandPose(position, Quaternion.identity);

        public static HandPose Lerp(HandPose from, HandPose to, float t)
        {
            return new HandPose(
                Vector3.Lerp(from.Position, to.Position, t),
                Quaternion.Slerp(from.Rotation, to.Rotation, t)
            );
        }
    }
}
