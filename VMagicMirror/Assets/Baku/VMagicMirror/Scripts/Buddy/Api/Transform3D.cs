using VMagicMirror.Buddy;

namespace Baku.VMagicMirror.Buddy.Api
{
    public class Transform3D : ITransform3D, IReadOnlyTransform3D
    {
        private readonly BuddyTransform3DInstance _instance;

        public Transform3D(BuddyTransform3DInstance instance)
        {
            _instance = instance;
        }

        // NOTE: 厳密にはITransform3Dと1:1対応じゃないが、実体がReadOnlyじゃないよ…という意味でこんくらいにしておく
        public override string ToString() => nameof(ITransform3D);

        public IReadOnlyTransform3D AsReadOnly() => this;

        public Vector3 LocalPosition
        {
            get => _instance.LocalPosition.ToApiValue();
            set => _instance.LocalPosition = value.ToEngineValue();
        }

        public Quaternion LocalRotation
        {
            get => _instance.LocalRotation.ToApiValue();
            set => _instance.LocalRotation = value.ToEngineValue();
        }

        public Vector3 LocalScale
        {
            get => _instance.LocalScale.ToApiValue();
            set => _instance.LocalScale = value.ToEngineValue();
        }

        public Vector3 Position
        {
            get => _instance.Position.ToApiValue();
            set => _instance.Position = value.ToEngineValue();
        }

        public Quaternion Rotation
        {
            get => _instance.Rotation.ToApiValue();
            set => _instance.Rotation = value.ToEngineValue();
        }

        public HumanBodyBones AttachedBone
        {
            get
            {
                if (_instance.ParentType != BuddyTransform3DInstance.ParentTypes.AvatarBone)
                {
                    return HumanBodyBones.None;
                }

                var bone = _instance.ParentBone.Value;
                return bone == UnityEngine.HumanBodyBones.LastBone
                    ? HumanBodyBones.None 
                    : bone.ToApiValue();
            }
        }

        public void SetParent(IReadOnlyTransform3D parent)
        {
            switch (parent)
            {
                case ManifestTransform3D manifestTransform3D:
                    _instance.SetParent(manifestTransform3D.GetInstance());
                    break;
                case Transform3D transform3D:
                    _instance.SetParent(transform3D._instance);
                    break;
                default:
                    _instance.RemoveParent();
                    break;
            }
        }

        public void SetParent(ITransform3D parent)
        {
            switch (parent)
            {
                case Transform3D transform3D:
                    _instance.SetParent(transform3D._instance);
                    break;
                default:
                    _instance.RemoveParent();
                    break;
            }
        }

        public void SetParent(HumanBodyBones bone)
        {
            if (bone is HumanBodyBones.None or HumanBodyBones.LastBone)
            {
                _instance.RemoveParent();
            }
            else
            {
                _instance.SetParentBone(bone.ToEngineValue());
            }
        }

        public void RemoveParent() => _instance.RemoveParent();
    }
}
