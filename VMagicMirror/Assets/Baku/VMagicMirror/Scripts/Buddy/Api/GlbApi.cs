using Baku.VMagicMirror.Buddy.Api.Interface;
using Quaternion = Baku.VMagicMirror.Buddy.Api.Interface.Quaternion;
using Vector3 = Baku.VMagicMirror.Buddy.Api.Interface.Vector3;

namespace Baku.VMagicMirror.Buddy.Api
{
    public class GlbApi : IGlbApi
    {
        public GlbApi(BuddyGlbInstance instance)
        {
            _instance = instance;
        }

        private readonly BuddyGlbInstance _instance;
        
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

        public Vector2 LocalScale
        {
            get => _instance.LocalScale.ToApiValue();
            set => _instance.LocalScale = value.ToEngineValue();
        }

        public Vector3 GetPosition() => _instance.GetWorldPosition().ToApiValue();
        public Quaternion GetRotation() => _instance.GetWorldRotation().ToApiValue();
        public void SetPosition(Vector3 position) => _instance.SetWorldPosition(position.ToEngineValue());
        public void SetRotation(Quaternion rotation) => _instance.SetWorldRotation(rotation.ToEngineValue());

        public void SetParent(ITransform3DApi parent)
        {
            var parentInstance = ((Transform3DApi)parent).GetInstance();
            _instance.SetParent(parentInstance);
        }

        public void Load(string path) => _instance.Load(path);
        public void Show() => _instance.Show();
        public void Hide() => _instance.Hide();

        public string[] GetAnimationNames() => _instance.GetAnimationNames();
        public void RunAnimation(string name) => _instance.RunAnimation(name, false, true);
        public void StopAnimation() => _instance.StopAnimation();
    }
}
