using Baku.VMagicMirror.Buddy;
using Zenject;

namespace Baku.VMagicMirror
{
    // NOTE: 初期実装ではTransform3Dしか入ってないが、このクラスからVrm / Glb / Sprite3Dのインスタンスも生成したい
    public class Buddy3DInstanceCreator
    {
        private readonly IFactory<BuddyTransform3DInstance> _transform3DInstanceFactory;
            
        [Inject]
        public Buddy3DInstanceCreator(IFactory<BuddyTransform3DInstance> transform3DInstanceFactory)
        {
            _transform3DInstanceFactory = transform3DInstanceFactory;
        }

        public BuddyTransform3DInstance CreateTransform3D() => _transform3DInstanceFactory.Create();
    }
}
