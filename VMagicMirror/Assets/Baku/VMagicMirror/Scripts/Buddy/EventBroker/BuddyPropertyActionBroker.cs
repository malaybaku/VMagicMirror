using System;
using R3;

namespace Baku.VMagicMirror.Buddy
{
    public class BuddyPropertyActionBroker
    {
        private readonly Subject<BuddyPropertyAction> _actionRequested = new();
        public IObservable<string> ActionRequestedForBuddy(BuddyId id) => _actionRequested
            .Where(a => a.BuddyId.Equals(id))
            .Select(a => a.PropertyName);
        
        public void RequestAction(BuddyPropertyAction action) => _actionRequested.OnNext(action);
    }

    public readonly struct BuddyPropertyAction
    {
        public BuddyId BuddyId { get; }
        public string PropertyName { get; }

        public BuddyPropertyAction(BuddyId buddyId, string propertyName)
        {
            BuddyId = buddyId;
            PropertyName = propertyName;
        }
    }
}
