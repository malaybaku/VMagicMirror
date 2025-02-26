using System.Collections.Generic;
using System.Linq;
using Object = UnityEngine.Object;

namespace Baku.VMagicMirror.Buddy
{
    /// <summary> Buddyに対し、Script APIに基づいて生成されたオブジェクトを格納しておくレポジトリ </summary>
    public class BuddyObjectInstanceRepository
    {
        private Dictionary<string, SingleBuddyObjectInstanceRepository> _repos = new();

        public IEnumerable<BuddyGlbInstance> GetAllGlbInstances()
            => _repos.Values.SelectMany(r => r.Glbs);

        public IEnumerable<BuddyVrmInstance> GetAllVrmInstances() 
            => _repos.Values.SelectMany(r => r.Vrms);

        public void AddSprite2D(string buddyId, BuddySpriteInstance instance) 
            => GetOrCreate(buddyId).AddSprite2D(instance);

        public void AddSprite3D(string buddyId, BuddySprite3DInstance instance)
            => GetOrCreate(buddyId).AddSprite3D(instance);

        public void AddGlb(string buddyId, BuddyGlbInstance instance)
            => GetOrCreate(buddyId).AddGlb(instance);

        public void AddVrm(string buddyId, BuddyVrmInstance instance)
            => GetOrCreate(buddyId).AddVrm(instance);

        public void DeleteBuddy(string buddyId)
        {
            if (Get(buddyId) is { } repo)
            {
                repo.DeleteAllObjects();
                _repos.Remove(buddyId);
            }
        }

        private SingleBuddyObjectInstanceRepository GetOrCreate(string buddyId)
        {
            buddyId = buddyId.ToLower();
            if (_repos.TryGetValue(buddyId, out var cached))
            {
                return cached;
            }

            var repo = new SingleBuddyObjectInstanceRepository(buddyId);
            _repos[buddyId] = repo;
            return repo;
        }

        private SingleBuddyObjectInstanceRepository Get(string buddyId)
            => _repos.GetValueOrDefault(buddyId, null);
    }

    public class SingleBuddyObjectInstanceRepository
    {
        public SingleBuddyObjectInstanceRepository(string buddyId)
        {
            BuddyId = buddyId;
        }

        public string BuddyId { get; }

        private List<BuddySpriteInstance> _sprite2Ds = new();
        private List<BuddySprite3DInstance> _sprite3Ds = new();
        private List<BuddyGlbInstance> _glbs = new();
        private List<BuddyVrmInstance> _vrms = new();

        public IReadOnlyList<BuddySpriteInstance> Sprite2Ds => _sprite2Ds;
        public IReadOnlyList<BuddySprite3DInstance> Sprite3Ds => _sprite3Ds;
        public IReadOnlyList<BuddyGlbInstance> Glbs => _glbs;
        public IReadOnlyList<BuddyVrmInstance> Vrms => _vrms;

        public void AddSprite2D(BuddySpriteInstance instance) => _sprite2Ds.Add(instance);
        public void AddSprite3D(BuddySprite3DInstance instance) => _sprite3Ds.Add(instance);
        public void AddGlb(BuddyGlbInstance instance) => _glbs.Add(instance);
        public void AddVrm(BuddyVrmInstance instance) => _vrms.Add(instance);

        public void DeleteAllObjects()
        {
            foreach (var i in _sprite2Ds)
            {
                Object.Destroy(i.gameObject);
            }
            foreach (var i in _sprite3Ds)
            {
                Object.Destroy(i.gameObject);
            }
            foreach (var i in _glbs)
            {
                Object.Destroy(i.gameObject);
            }
            foreach (var i in _vrms)
            {
                Object.Destroy(i.gameObject);
            }
            
            _sprite2Ds.Clear();
            _sprite3Ds.Clear();
            _glbs.Clear();
            _vrms.Clear();
        }
    }
}
