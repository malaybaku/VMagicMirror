using System.Collections.Generic;
using System.Linq;
using Object = UnityEngine.Object;

namespace Baku.VMagicMirror.Buddy
{
    /// <summary> Buddyに対し、Script APIに基づいて生成されたオブジェクトを格納しておくレポジトリ </summary>
    public class BuddyRuntimeObjectRepository
    {
        private readonly Dictionary<string, SingleBuddyObjectInstanceRepository> _repo = new();

        public IEnumerable<BuddyGlbInstance> GetAllGlbInstances()
            => _repo.Values.SelectMany(r => r.Glbs);

        public IEnumerable<BuddyVrmInstance> GetAllVrmInstances() 
            => _repo.Values.SelectMany(r => r.Vrms);

        public void AddSprite2D(BuddySprite2DInstance instance) 
            => GetOrCreate(instance.BuddyId).AddSprite2D(instance);

        public void AddSprite3D(BuddySprite3DInstance instance)
            => GetOrCreate(instance.BuddyId).AddSprite3D(instance);

        public void AddGlb(BuddyGlbInstance instance)
            => GetOrCreate(instance.BuddyId).AddGlb(instance);

        public void AddVrm(BuddyVrmInstance instance)
            => GetOrCreate(instance.BuddyId).AddVrm(instance);

        public void DeleteBuddy(string buddyId)
        {
            if (Get(buddyId) is { } repo)
            {
                repo.DeleteAllObjects();
                _repo.Remove(buddyId);
            }
        }

        private SingleBuddyObjectInstanceRepository GetOrCreate(string buddyId)
        {
            buddyId = buddyId.ToLower();
            if (_repo.TryGetValue(buddyId, out var cached))
            {
                return cached;
            }

            var repo = new SingleBuddyObjectInstanceRepository(buddyId);
            _repo[buddyId] = repo;
            return repo;
        }

        private SingleBuddyObjectInstanceRepository Get(string buddyId)
            => _repo.GetValueOrDefault(buddyId, null);
    }

    public class SingleBuddyObjectInstanceRepository
    {
        public SingleBuddyObjectInstanceRepository(string buddyId)
        {
            BuddyId = buddyId;
        }

        public string BuddyId { get; }

        private List<BuddySprite2DInstance> _sprite2Ds = new();
        private List<BuddySprite3DInstance> _sprite3Ds = new();
        private List<BuddyGlbInstance> _glbs = new();
        private List<BuddyVrmInstance> _vrms = new();

        public IReadOnlyList<BuddySprite2DInstance> Sprite2Ds => _sprite2Ds;
        public IReadOnlyList<BuddySprite3DInstance> Sprite3Ds => _sprite3Ds;
        public IReadOnlyList<BuddyGlbInstance> Glbs => _glbs;
        public IReadOnlyList<BuddyVrmInstance> Vrms => _vrms;

        public void AddSprite2D(BuddySprite2DInstance instance) => _sprite2Ds.Add(instance);
        public void AddSprite3D(BuddySprite3DInstance instance) => _sprite3Ds.Add(instance);
        public void AddGlb(BuddyGlbInstance instance) => _glbs.Add(instance);
        public void AddVrm(BuddyVrmInstance instance) => _vrms.Add(instance);

        public void DeleteAllObjects()
        {
            foreach (var i in _sprite2Ds)
            {
                // NOTE: 親子関係がついたオブジェクトの親が先に破棄される可能性があることに留意している。他も同様
                // これでまだ不安定になる場合、Dispose/Destroyの前にSetParent(null)できるようなフローを検討してもよい
                i.Dispose();
                if (i != null)
                {
                    Object.Destroy(i.gameObject);
                }
            }
            foreach (var i in _sprite3Ds)
            {
                i.Dispose();
                if (i != null)
                {
                    Object.Destroy(i.gameObject);
                }
            }
            foreach (var i in _glbs)
            {
                i.Dispose();
                if (i != null)
                {
                    Object.Destroy(i.gameObject);
                }
            }
            foreach (var i in _vrms)
            {
                i.Dispose();
                if (i != null)
                {
                    Object.Destroy(i.gameObject);
                }
            }
            
            _sprite2Ds.Clear();
            _sprite3Ds.Clear();
            _glbs.Clear();
            _vrms.Clear();
        }
    }
}
