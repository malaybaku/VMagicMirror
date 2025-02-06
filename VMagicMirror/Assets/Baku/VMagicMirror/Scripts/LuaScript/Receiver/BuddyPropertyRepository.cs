using System.Collections.Generic;
using Baku.VMagicMirror.LuaScript.Api;

namespace Baku.VMagicMirror.LuaScript
{
    /// <summary>
    /// Buddyのプロパティを保持しておくクラス。
    /// </summary>
    public class BuddyPropertyRepository
    {
        //NOTE: 「一度作ったインスタンスを消す事がある」という構造だと参照維持が面倒だし、
        //それがメモリ的に不利になるケースは珍しいのでケアは頑張らない
        private readonly Dictionary<string, PropertyApi> _propertyApis = new();

        public PropertyApi Get(string buddyId)
        {
            if (_propertyApis.TryGetValue(buddyId, out var existingApi))
            {
                return existingApi;
            }
            
            var api = new PropertyApi();
            _propertyApis[buddyId] = api;
            return api;
        }
    }
}
