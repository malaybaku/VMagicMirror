using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using Object = UnityEngine.Object;

namespace Baku.VMagicMirror.Buddy
{
    public class BuddyTransformInstanceRepository
    {
        private readonly Dictionary<string, SingleBuddyTransforms> _transforms = new();

        private readonly Subject<BuddyTransform2DInstance> _added2D = new();
        public IObservable<BuddyTransform2DInstance> Added2D => _added2D;

        public IEnumerable<BuddyTransform2DInstance> GetTransform2DInstances()
            => _transforms.Values.SelectMany(ts => ts.GetTransform2DInstances());
        
        public void AddTransform2D(string buddyId, string name, BuddyTransform2DInstance instance)
        {
            if (!_transforms.TryGetValue(buddyId, out var transforms))
            {
                transforms = new SingleBuddyTransforms();
                _transforms[buddyId] = transforms;
            }
            transforms.AddTransform2D(name, instance);
            _added2D.OnNext(instance);
        }
        
        public bool TryGetTransform2D(string buddyId, string name, out BuddyTransform2DInstance result)
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

        public void DeleteInstance(string buddyId)
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
            private readonly Dictionary<string, BuddyTransform2DInstance> _transform2Ds = new();
            //3Dも併設する

            public IEnumerable<BuddyTransform2DInstance> GetTransform2DInstances() => _transform2Ds.Values;
            
            public void AddTransform2D(string name, BuddyTransform2DInstance instance)
            {
                // キーが被ってるのにここに到達している場合は以下いずれかが起こっている
                // - manifest.jsonのパース時に名称の重複チェックができてない
                // - Disposeのし忘れ
                if (!_transform2Ds.TryAdd(name, instance))
                {
                    throw new InvalidOperationException("Specified instance already exists");
                }
            }

            public bool TryGetTransform2D(string name, out BuddyTransform2DInstance result)
                => _transform2Ds.TryGetValue(name, out result);
            
            public void Dispose()
            {
                foreach (var instance in _transform2Ds.Values)
                {
                    Object.Destroy(instance.gameObject);
                }
                _transform2Ds.Clear();
            }
        }
    }
}
