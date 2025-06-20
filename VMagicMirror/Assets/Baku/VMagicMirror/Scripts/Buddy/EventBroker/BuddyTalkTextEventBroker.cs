using System;
using UniRx;

namespace Baku.VMagicMirror.Buddy
{
    public class BuddyTalkTextEventBroker
    {
        private readonly Subject<TalkTextItemInternal> _itemDequeued = new();
        public IObservable<TalkTextItemInternal> ItemDequeued(BuddyId id) => _itemDequeued
            .Where(item => item.BuddyId.Equals(id));
        
        private readonly Subject<TalkTextItemInternal> _itemFinished = new();
        public IObservable<TalkTextItemInternal> ItemFinished(BuddyId id) => _itemFinished
            .Where(item => item.BuddyId.Equals(id));
        
        public void DequeueItem(TalkTextItemInternal item) => _itemDequeued.OnNext(item);
        public void FinishItem(TalkTextItemInternal item) => _itemFinished.OnNext(item);
    }
}
