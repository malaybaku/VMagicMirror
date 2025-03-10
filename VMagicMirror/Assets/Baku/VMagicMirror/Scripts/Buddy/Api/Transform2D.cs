using VMagicMirror.Buddy;

namespace Baku.VMagicMirror.Buddy.Api
{
    public class Transform2D : ITransform2D, IReadOnlyTransform2D
    {
        private readonly BuddyTransform2DInstance _instance;

        public Transform2D(BuddyTransform2DInstance instance)
        {
            _instance = instance;
        }

        IReadOnlyTransform2D ITransform2D.AsReadOnly() => this;

        public Vector2 LocalPosition
        {
            get => _instance.LocalPosition.ToApiValue();
            set => _instance.LocalPosition = value.ToEngineValue();
        }

        public Quaternion LocalRotation
        {
            get => _instance.LocalRotation.ToApiValue();
            set => _instance.LocalRotation = value.ToEngineValue();
        }

        public Vector2 LocalScale
        {
            get => _instance.LocalScale.ToApiValue();
            set => _instance.LocalScale = value.ToEngineValue();
        }

        public Vector2 Pivot
        {
            get => _instance.Pivot.ToApiValue();
            set => _instance.Pivot = value.ToEngineValue();
        }

        public Vector2 Position
        {
            get => _instance.Position.ToApiValue();
            set => _instance.Position = value.ToEngineValue();
        }

        public Quaternion Rotation
        {
            get => _instance.Rotation.ToApiValue();
            set => _instance.Rotation = value.ToEngineValue();
        }
        
        public void SetParent(IReadOnlyTransform2D parent)
        {
            switch (parent)
            {
                case ManifestTransform2D manifestTransform2D:
                    _instance.SetParent(manifestTransform2D.GetInstance());
                    break;
                case Transform2D transform2D:
                    _instance.SetParent(transform2D._instance);
                    break;
                default:
                    _instance.RemoveParent();
                    break;
            }
        }
        
        public void SetParent(ITransform2D parent)
        {
            switch (parent)
            {
                case Transform2D transform2D:
                    _instance.SetParent(transform2D._instance);
                    break;
                default:
                    _instance.RemoveParent();
                    break;
            }
        }

        public void RemoveParent() => _instance.RemoveParent();
    }
}
