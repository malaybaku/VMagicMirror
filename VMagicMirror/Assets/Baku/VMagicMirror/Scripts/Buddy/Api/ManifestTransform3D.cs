using VMagicMirror.Buddy;

namespace Baku.VMagicMirror.Buddy.Api
{
    public class ManifestTransform3D : IReadOnlyTransform3D
    {
        public ManifestTransform3D(BuddyManifestTransform3DInstance instance)
        {
            _instance = instance;
        }

        private readonly BuddyManifestTransform3DInstance _instance;
        internal BuddyManifestTransform3DInstance GetInstance() => _instance;

        public override string ToString() => nameof(IReadOnlyTransform3D);
        
        public Vector3 LocalPosition => _instance.LocalPosition.ToApiValue();
        public Quaternion LocalRotation => _instance.LocalRotation.ToApiValue();
        public Vector3 Position => _instance.Position.ToApiValue();
        public Quaternion Rotation => _instance.Rotation.ToApiValue();
        public Vector3 LocalScale => _instance.LocalScale.ToApiValue();

        public HumanBodyBones AttachedBone
        {
            get
            {
                if (_instance.HasParentBone)
                {
                    return (HumanBodyBones)_instance.ParentBone;
                }
                else
                {
                    return HumanBodyBones.None;
                }
            }
        }
    }
}
