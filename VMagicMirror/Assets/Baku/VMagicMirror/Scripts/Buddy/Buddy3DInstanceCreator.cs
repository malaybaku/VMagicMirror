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
        private readonly BuddyPresetResources _buddyPresetResources;

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
        
        private readonly Subject<BuddyVrmAnimationInstance> _vrmAnimationCreated = new();
        public IObservable<BuddyVrmAnimationInstance> VrmAnimationCreated => _vrmAnimationCreated;
        
        [Inject]
        public Buddy3DInstanceCreator(
            IFactory<BuddyManifestTransform3DInstance> transform3DInstanceFactory,
            IFactory<BuddySprite3DInstance> sprite3DInstanceFactory,
            BuddyPresetResources presetResources
            )
        {
            _transform3DInstanceFactory = transform3DInstanceFactory;
            _sprite3DInstanceFactory = sprite3DInstanceFactory;
            _buddyPresetResources = presetResources;
        }

        // NOTE: この関数だけScript APIとは別のタイミングで(スクリプトの起動の直前くらいに)呼ばれる
        public BuddyManifestTransform3DInstance CreateManifestTransform3D(BuddyId buddyId, string instanceName)
        {
            var result = _transform3DInstanceFactory.Create();
            result.BuddyId = buddyId;
            result.InstanceName = instanceName;
            return result;
        }

        public BuddySprite3DInstance CreateSprite3DInstance(BuddyFolder buddyFolder)
        {
            var result = _sprite3DInstanceFactory.Create();
            result.BuddyFolder = buddyFolder;
            result.PresetResources = _buddyPresetResources;
            _transform3dCreated.OnNext(result.Transform3DInstance);
            _sprite3dCreated.OnNext(result);
            return result;
        }

        // NOTE: GLBとかVRMには動的読み込み要素しかないので、ファクトリは使わない
        public BuddyVrmInstance CreateVrmInstance(BuddyFolder buddyFolder)
        {
            var obj = new GameObject(nameof(BuddyVrmInstance));
            var result = obj.AddComponent<BuddyVrmInstance>();
            result.BuddyFolder = buddyFolder;
            _transform3dCreated.OnNext(result.GetTransform3D());
            _vrmCreated.OnNext(result);
            return result;
        }

        public BuddyGlbInstance CreateGlbInstance(BuddyFolder buddyFolder)
        {
            var obj = new GameObject(nameof(BuddyGlbInstance));
            var result = obj.AddComponent<BuddyGlbInstance>();
            result.BuddyFolder = buddyFolder;
            _transform3dCreated.OnNext(result.GetTransform3D());
            _glbCreated.OnNext(result);
            return result;
        }

        // NOTE: VrmAnimationはそれ自体をギズモとかで制御するわけではないが、ロード/アンロードの特性がVRM/GLBに似ているのでここで生成する
        public BuddyVrmAnimationInstance CreateVrmAnimationInstance(BuddyFolder buddyFolder)
        {
            var result = new BuddyVrmAnimationInstance
            {
                BuddyFolder = buddyFolder,
            };
            _vrmAnimationCreated.OnNext(result);
            return result; 
        }
    }
}
