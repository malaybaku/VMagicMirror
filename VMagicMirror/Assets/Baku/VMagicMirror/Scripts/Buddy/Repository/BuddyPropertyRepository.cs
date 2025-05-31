using System.Collections.Generic;
using UnityEngine;

namespace Baku.VMagicMirror.Buddy
{
    /// <summary>
    /// Buddyのプロパティを保持しておくクラス。
    /// </summary>
    public class BuddyPropertyRepository
    {
        //NOTE: 「一度作ったインスタンスを消す事がある」という構造だと参照維持が面倒だし、
        //それがメモリ的に不利になるケースは珍しいのでケアは頑張らない
        private readonly Dictionary<BuddyId, SingleBuddyPropertyRepository> _repositories = new();

        public SingleBuddyPropertyRepository GetOrCreate(BuddyId buddyId)
        {
            if (_repositories.TryGetValue(buddyId, out var cached))
            {
                return cached;
            }
            
            var repo = new SingleBuddyPropertyRepository();
            _repositories[buddyId] = repo;
            return repo;
        }
    }

    public class SingleBuddyPropertyRepository
    {
        // NOTE:
        // - WPF側で初期化のシーケンスとかメッセージの送信順を担保する前提のため、IsInitialized() みたいなのは持たない
        // - Instance自体のライフサイクルは長い(Buddyのoff/onとかでは再生成されない)。
        //   - これは、Buddyの起動前でもプロパティが同期できるほうが実装がトータルでラクそうなため。
        private readonly Dictionary<string, object> _values = new();

        public void AddOrUpdate(BuddyProperty property) => _values[property.Name] = property.Value;
        public void Clear() => _values.Clear();

        // NOTE: boolじゃない場合、一様にfalse扱いされる。他の型も考え方は同様
        public bool GetBool(string key) => Get(key) is true;
        public int GetInt(string key) => Get(key) is int v ? v : 0;
        public float GetFloat(string key) => Get(key) is float v ? v : 0f;
        public string GetString(string key) => Get(key) is string v ? v : "";
        public Vector2 GetVector2(string key) => Get(key) is Vector2 v ? v : Vector2.zero;
        public Vector3 GetVector3(string key) => Get(key) is Vector3 v ? v : Vector3.zero;
        public Quaternion GetQuaternion(string key) => Get(key) is Quaternion v ? v : Quaternion.identity;

        private object Get(string key) => _values.GetValueOrDefault(key);
    }
}
