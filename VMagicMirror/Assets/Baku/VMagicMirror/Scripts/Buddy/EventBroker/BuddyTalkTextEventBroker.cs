using System;
using Baku.VMagicMirror.Buddy.Api;
using UniRx;
using VMagicMirror.Buddy;

namespace Baku.VMagicMirror.Buddy
{
    public class BuddyTalkTextEventBroker
    {
        private readonly Subject<TalkTextItemInternal> _itemDequeued = new();
        private readonly Subject<TalkTextItemInternal> _itemFinished = new();
        public IObservable<(TalkTextApi api, ITalkTextItem item)> ItemDequeued(BuddyId id) => ForBuddy(_itemDequeued, id);
        public IObservable<(TalkTextApi api, ITalkTextItem item)> ItemFinished(BuddyId id) => ForBuddy(_itemFinished, id);

        private IObservable<(TalkTextApi api, ITalkTextItem item)> ForBuddy(IObservable<TalkTextItemInternal> src, BuddyId buddyId) => src
            .Where(item => item.BuddyId.Equals(buddyId))
            .Select(item => (item.Api, item.ToApiValue()));

        public void DequeueItem(TalkTextItemInternal item) => _itemDequeued.OnNext(item);
        public void FinishItem(TalkTextItemInternal item) => _itemFinished.OnNext(item);
    }
}
