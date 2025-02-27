using Baku.VMagicMirror.Buddy;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    // NOTE: 初期実装ではTransform3Dしか入ってないが、このクラスからVrm / Glb / Sprite3Dのインスタンスも生成したい
    public class Buddy3DInstanceCreator
    {
        private readonly IFactory<BuddyTransform3DInstance> _transform3DInstanceFactory;
        private readonly IFactory<BuddySprite3DInstance> _sprite3DInstanceFactory;
        
            
        [Inject]
        public Buddy3DInstanceCreator(
            IFactory<BuddyTransform3DInstance> transform3DInstanceFactory,
            IFactory<BuddySprite3DInstance> sprite3DInstanceFactory)
        {
            _transform3DInstanceFactory = transform3DInstanceFactory;
            _sprite3DInstanceFactory = sprite3DInstanceFactory;
        }

        public BuddyTransform3DInstance CreateTransform3D() => _transform3DInstanceFactory.Create();

        public BuddySprite3DInstance CreateSprite3DInstance() => _sprite3DInstanceFactory.Create();
        
        // NOTE: GLBとかVRMには動的読み込み要素しかないので、ファクトリは使わない
        public BuddyVrmInstance CreateVrmInstance()
        {
            var obj = new GameObject(nameof(BuddyVrmInstance));
            return obj.AddComponent<BuddyVrmInstance>();
        }

        public BuddyGlbInstance CreateGlbInstance()
        {
            var obj = new GameObject(nameof(BuddyGlbInstance));
            return obj.AddComponent<BuddyGlbInstance>();
        }

    }
}
