using Baku.VMagicMirror.Buddy.Api.Interface;

namespace Baku.VMagicMirror.Buddy.Api
{
    //TODO: GuiAreaもTransform2Dの一種として扱うようにする過程で汎用化がいりそう
    // 3Dと同じく、Transform2DInstanceみたいな別コンポーネントを見に行くように設計をいじる…というのが一番ありそう

    /// <summary>
    /// Spriteの実装に
    /// </summary>
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

        //TODO? AsReadOnlyとか増えそう
        
        public void SetParent(IReadOnlyTransform2D parent)
        {
            var parentInstance = ((ManifestTransform2D)parent).GetInstance();
            _instance.SetParent(parentInstance);
        }
        
        public void SetParent(ITransform2D parent)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveParent() => _instance.RemoveParent();
    }
}
