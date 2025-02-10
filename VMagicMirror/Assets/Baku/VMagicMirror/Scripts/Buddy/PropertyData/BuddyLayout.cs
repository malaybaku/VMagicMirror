using UnityEngine;

namespace Baku.VMagicMirror.Buddy
{
    public readonly struct BuddyTransform2DLayout
    {
        public BuddyTransform2DLayout(Vector2 position, Vector3 rotationEuler, float scale)
        {
            Position = position;
            RotationEuler = rotationEuler;
            Scale = scale;
        }

        public Vector2 Position { get; }
        public Vector3 RotationEuler { get; }
        public float Scale { get; }
    }
    
    public readonly struct BuddyTransform3DLayout
    {
        public BuddyTransform3DLayout(Vector3 position, Quaternion rotation, float scale, HumanBodyBones? parentBone)
        {
            Position = position;
            Rotation = rotation;
            Scale = scale;
            HasParentBone = parentBone.HasValue;
            ParentBone = parentBone.GetValueOrDefault();
        }

        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public float Scale { get; }
        
        public bool HasParentBone { get; }
        public HumanBodyBones ParentBone { get; }
    }
}
