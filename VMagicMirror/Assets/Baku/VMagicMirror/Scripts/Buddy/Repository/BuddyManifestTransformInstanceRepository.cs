using System;
using System.Collections.Generic;
using System.Linq;
using R3;
using Zenject;
using Object = UnityEngine.Object;

namespace Baku.VMagicMirror.Buddy
{
    public class BuddyManifestTransformInstanceRepository
    {
        [Inject]
        public BuddyManifestTransformInstanceRepository() { }
        
        private readonly Dictionary<BuddyId, SingleBuddyTransforms> _transforms = new();

        private readonly Subject<BuddyManifestTransform2DInstance> _transform2DAdded = new();
        public IObservable<BuddyManifestTransform2DInstance> Transform2DAdded => _transform2DAdded;

        private readonly Subject<BuddyManifestTransform3DInstance> _transform3DAdded = new();
        public IObservable<BuddyManifestTransform3DInstance> Transform3DAdded => _transform3DAdded;

        public IEnumerable<BuddyManifestTransform2DInstance> GetTransform2DInstances()
            => _transforms.Values.SelectMany(ts => ts.GetTransform2DInstances());

        public IEnumerable<BuddyManifestTransform3DInstance> GetTransform3DInstances()
            => _transforms.Values.SelectMany(ts => ts.GetTransform3DInstances());
        
        public void AddTransform2D(BuddyId buddyId, string name, BuddyManifestTransform2DInstance instance)
        {
            if (!_transforms.TryGetValue(buddyId, out var transforms))
            {
                transforms = new SingleBuddyTransforms();
                _transforms[buddyId] = transforms;
            }
            transforms.AddTransform2D(name, instance);
            _transform2DAdded.OnNext(instance);
        }
        
        public void AddTransform3D(BuddyId buddyId, string name, BuddyManifestTransform3DInstance instance)
        {
            if (!_transforms.TryGetValue(buddyId, out var transforms))
            {
                transforms = new SingleBuddyTransforms();
                _transforms[buddyId] = transforms;
            }
            transforms.AddTransform3D(name, instance);
            _transform3DAdded.OnNext(instance);
        }

        public bool TryGetTransform2D(BuddyId buddyId, string name, out BuddyManifestTransform2DInstance result)
        {
            if (_transforms.TryGetValue(buddyId, out var transforms) &&
                transforms.TryGetTransform2D(name, out var existingResult))
            {
                result = existingResult;
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        public bool TryGetTransform3D(BuddyId buddyId, string name, out BuddyManifestTransform3DInstance result)
        {
            if (_transforms.TryGetValue(buddyId, out var transforms) &&
                transforms.TryGetTransform3D(name, out var existingResult))
            {
                result = existingResult;
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        public void DeleteInstance(BuddyId buddyId)
        {
            if (!_transforms.TryGetValue(buddyId, out var transforms))
            {
                return;
            }
            
            transforms.Dispose();
            _transforms.Remove(buddyId);
        }

        class SingleBuddyTransforms
        {
            private readonly Dictionary<string, BuddyManifestTransform2DInstance> _transform2Ds = new();
            private readonly Dictionary<string, BuddyManifestTransform3DInstance> _transform3Ds = new();

            public IEnumerable<BuddyManifestTransform2DInstance> GetTransform2DInstances() => _transform2Ds.Values;
            public IEnumerable<BuddyManifestTransform3DInstance> GetTransform3DInstances() => _transform3Ds.Values;
            
            public void AddTransform2D(string name, BuddyManifestTransform2DInstance instance)
            {
                // キーが被ってるのにここに到達している場合は以下いずれかが起こっている
                // - manifest.jsonのパース時に名称の重複チェックができてない
                // - Disposeのし忘れ
                if (!_transform2Ds.TryAdd(name, instance))
                {
                    throw new InvalidOperationException("Specified instance already exists");
                }
            }

            public void AddTransform3D(string name, BuddyManifestTransform3DInstance instance)
            {
                if (!_transform3Ds.TryAdd(name, instance))
                {
                    throw new InvalidOperationException("Specified instance already exists");
                }
            }

            public bool TryGetTransform2D(string name, out BuddyManifestTransform2DInstance result)
                => _transform2Ds.TryGetValue(name, out result);

            public bool TryGetTransform3D(string name, out BuddyManifestTransform3DInstance result)
                => _transform3Ds.TryGetValue(name, out result);
            
            public void Dispose()
            {
                foreach (var instance in _transform2Ds.Values)
                {
                    Object.Destroy(instance.gameObject);
                }
                _transform2Ds.Clear();
                
                foreach (var instance in _transform3Ds.Values)
                {
                    Object.Destroy(instance.gameObject);
                }
                _transform3Ds.Clear();
            }
        }
    }
}
