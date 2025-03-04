using Baku.VMagicMirror.Buddy.Api.Interface;

namespace Baku.VMagicMirror.Buddy.Api
{
    public class Transform3DApi : ITransform3DApi
    {
        public Transform3DApi(BuddyTransform3DInstance instance)
        {
            _instance = instance;
        }

        private readonly BuddyTransform3DInstance _instance;
        internal BuddyTransform3DInstance GetInstance() => _instance;
        
        public Vector3 LocalPosition => _instance.LocalPosition.ToApiValue();
        public Quaternion LocalRotation => _instance.LocalRotation.ToApiValue();
        public Vector3 Position => _instance.Position.ToApiValue();
        public Quaternion Rotation => _instance.Rotation.ToApiValue();
        public float Scale => _instance.Scale;

        public HumanBodyBones AttachedBone
        {
            get
            {
                if (_instance.AttachedBone is { } bone)
                {
                    return (HumanBodyBones)bone;
                }
                else
                {
                    return HumanBodyBones.None;
                }
            }
        }
    }
}
