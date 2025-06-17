using System;
using UniRx;

namespace Baku.VMagicMirror.Buddy
{
    public class BuddyTalkTextEventBroker
    {
        private readonly Subject<ITalkTextItemInternal> _itemDequeued = new();
        public IObservable<ITalkTextItemInternal> ItemDequeued(BuddyId id) => _itemDequeued
            .Where(item => item.BuddyId.Equals(id));
        
        private readonly Subject<ITalkTextItemInternal> _itemFinished = new();
        public IObservable<ITalkTextItemInternal> ItemFinished(BuddyId id) => _itemFinished
            .Where(item => item.BuddyId.Equals(id));
        
        public void DequeueItem(ITalkTextItemInternal item) => _itemDequeued.OnNext(item);
        public void FinishItem(ITalkTextItemInternal item) => _itemFinished.OnNext(item);
    }
}
