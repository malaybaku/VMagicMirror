using System;
using Baku.VMagicMirror.Buddy.Api.Interface;

namespace Baku.VMagicMirror.Buddy.Api
{
    public class Transform3D : ITransform3D, IReadOnlyTransform3D
    {
        private readonly BuddyTransform3DInstance _instance;

        public Transform3D(BuddyTransform3DInstance instance)
        {
            _instance = instance;
        }

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

        // TODO: SetParentの結果に即した値にしたい
        public HumanBodyBones AttachedBone
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void SetParent(IReadOnlyTransform3D parent)
        {
            var parentInstance = ((ManifestTransform3D)parent).GetInstance();
            _instance.SetParent(parentInstance);
        }

        public void SetParent(ITransform3D parent)
        {
            throw new System.NotImplementedException();
        }

        public void SetParent(HumanBodyBones bone)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveParent()
        {
            throw new System.NotImplementedException();
        }
    }
}
