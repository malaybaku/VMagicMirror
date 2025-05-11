using System.Collections.Generic;
using Baku.VMagicMirror.Buddy.Api;

namespace Baku.VMagicMirror.Buddy
{
    /// <summary>
    /// Buddyのプロパティを保持しておくクラス。
    /// </summary>
    public class BuddyPropertyRepository
    {
        //NOTE: 「一度作ったインスタンスを消す事がある」という構造だと参照維持が面倒だし、
        //それがメモリ的に不利になるケースは珍しいのでケアは頑張らない
        private readonly Dictionary<BuddyId, PropertyApi> _propertyApis = new();

        public PropertyApi Get(BuddyId buddyId)
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
