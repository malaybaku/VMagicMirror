using System.Collections.Generic;
using Baku.VMagicMirror.Buddy;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// Buddyに紐づくレイアウト情報をアプリケーションのライフサイクルにわたって保持するレポジトリ。
    /// 実際のGameObject等は保持しない (GameObjectの生成はScriptのロード時まで遅延させるため)
    /// </summary>
    public class BuddyLayoutRepository
    {
        //NOTE: 「一度作ったインスタンスを消す事がある」という構造だと参照維持が面倒だし、
        //それがメモリ的に不利になるケースは珍しいのでケアは頑張らない
        private readonly Dictionary<string, SingleBuddyLayoutRepository> _layouts = new();

        public SingleBuddyLayoutRepository Get(string buddyId)
        {
            if (_layouts.TryGetValue(buddyId, out var existingRepository))
            {
                return existingRepository;
            }
            
            var repository = new SingleBuddyLayoutRepository();
            _layouts[buddyId] = repository;
            return repository;
        }

        /// <summary>
        /// buddyのリロード時に特定のidのbuddyが無くなった(=フォルダが削除された)場合に呼び出してもよい
        /// 呼ばないでも基本的には破綻しない
        /// </summary>
        /// <param name="buddyId"></param>
        public void Remove(string buddyId) => _layouts.Remove(buddyId);
    }

    // NOTE: このクラス自体の戻り値はあくまで設定だけで、これを元に動かしたアイテム一覧がAPIになる
    public class SingleBuddyLayoutRepository
    {
        private readonly Dictionary<string, BuddyTransform2DLayout> _transform2Ds = new();
        private readonly Dictionary<string, BuddyTransform3DLayout> _transform3Ds = new();

        public IReadOnlyDictionary<string, BuddyTransform2DLayout> Transform2Ds => _transform2Ds;
        public IReadOnlyDictionary<string, BuddyTransform3DLayout> Transform3Ds => _transform3Ds;

        public void AddOrUpdate(string key, BuddyTransform2DLayout property)
            => _transform2Ds[key] = property;

        public void AddOrUpdate(string key, BuddyTransform3DLayout property)
            => _transform3Ds[key] = property;

        public void Clear()
        {
            _transform2Ds.Clear();
            _transform3Ds.Clear();
        }
    }
}
