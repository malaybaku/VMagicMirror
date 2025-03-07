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

        private readonly Subject<BuddySprite3DInstance> _sprite3dCreated = new();
        public IObservable<BuddySprite3DInstance> Sprite3DCreated => _sprite3dCreated;

        private readonly Subject<BuddyGlbInstance> _glbCreated = new();
        public IObservable<BuddyGlbInstance> GlbCreated => _glbCreated;

        private readonly Subject<BuddyVrmInstance> _vrmCreated = new();
        public IObservable<BuddyVrmInstance> VrmCreated => _vrmCreated;
        
        [Inject]
        public Buddy3DInstanceCreator(
            IFactory<BuddyManifestTransform3DInstance> transform3DInstanceFactory,
            IFactory<BuddySprite3DInstance> sprite3DInstanceFactory)
        {
            _transform3DInstanceFactory = transform3DInstanceFactory;
            _sprite3DInstanceFactory = sprite3DInstanceFactory;
        }

        // NOTE: この関数だけScript APIとは別のタイミングで(スクリプトの起動の直前くらいに)呼ばれる
        public BuddyManifestTransform3DInstance CreateManifestTransform3D(string buddyId, string instanceName)
        {
            var result = _transform3DInstanceFactory.Create();
            result.BuddyId = buddyId;
            result.InstanceName = instanceName;
            return result;
        }

        public BuddySprite3DInstance CreateSprite3DInstance(string buddyId)
        {
            var result = _sprite3DInstanceFactory.Create();
            result.BuddyId = buddyId;   
            _transform3dCreated.OnNext(result.Transform3DInstance);
            _sprite3dCreated.OnNext(result);
            return result;
        }

        // NOTE: GLBとかVRMには動的読み込み要素しかないので、ファクトリは使わない
        public BuddyVrmInstance CreateVrmInstance(string buddyId)
        {
            var obj = new GameObject(nameof(BuddyVrmInstance));
            var result = obj.AddComponent<BuddyVrmInstance>();
            result.BuddyId = buddyId;
            _transform3dCreated.OnNext(result.GetTransform3D());
            _vrmCreated.OnNext(result);
            return result;
        }

        public BuddyGlbInstance CreateGlbInstance(string buddyId)
        {
            var obj = new GameObject(nameof(BuddyGlbInstance));
            var result = obj.AddComponent<BuddyGlbInstance>();
            result.BuddyId = buddyId;
            _transform3dCreated.OnNext(result.GetTransform3D());
            _glbCreated.OnNext(result);
            return result;
        }
    }
}
