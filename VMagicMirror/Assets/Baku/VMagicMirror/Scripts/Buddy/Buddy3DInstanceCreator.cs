using System;
using Baku.VMagicMirror.Buddy;
using UniRx;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    // NOTE: 初期実装ではTransform3Dしか入ってないが、このクラスからVrm / Glb / Sprite3Dのインスタンスも生成したい
    public class Buddy3DInstanceCreator
    {
        private readonly IFactory<BuddyManifestTransform3DInstance> _transform3DInstanceFactory;
        private readonly IFactory<BuddySprite3DInstance> _sprite3DInstanceFactory;

        private readonly Subject<BuddyTransform3DInstance> _transform3dCreated = new();
        /// <summary>
        /// <see cref="CreateSprite3DInstance"/>, <see cref="CreateGlbInstance"/>, <see cref="CreateVrmInstance"/>
        /// のいずれかを呼び出してTransform3Dが生成されると発火します。
        /// オブジェクトの破棄に対しては何も発火しないことに注意して下さい。
        /// </summary>
        public IObservable<BuddyTransform3DInstance> Transform3DCreated => _transform3dCreated;
            
        [Inject]
        public Buddy3DInstanceCreator(
            IFactory<BuddyManifestTransform3DInstance> transform3DInstanceFactory,
            IFactory<BuddySprite3DInstance> sprite3DInstanceFactory)
        {
            _transform3DInstanceFactory = transform3DInstanceFactory;
            _sprite3DInstanceFactory = sprite3DInstanceFactory;
        }

        public BuddyManifestTransform3DInstance CreateTransform3D()
        {
            return _transform3DInstanceFactory.Create();
        }

        public BuddySprite3DInstance CreateSprite3DInstance()
        {
            var result = _sprite3DInstanceFactory.Create();
            _transform3dCreated.OnNext(result.Transform3DInstance);
            return result;
        }

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
