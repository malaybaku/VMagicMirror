using System;
using Baku.VMagicMirror.Buddy.Api;
using R3;
using VMagicMirror.Buddy;

namespace Baku.VMagicMirror.Buddy
{
    /// <summary>
    /// Sprite2D関連のイベントをSpriteEventInvokerCSharpにリダイレクトして発火タイミングとかを調整するためのBroker
    /// </summary>
    public class BuddySpriteEventBroker
    {
        public readonly struct SpriteEventData
        {
            public Sprite2DApi Api { get; }
            public Pointer2DDataInternal PointerData { get; }
            public BuddyId BuddyId => Api.BuddyFolder.BuddyId;

            public SpriteEventData(Sprite2DApi api, Pointer2DDataInternal pointerData)
            {
                Api = api;
                PointerData = pointerData;
            }
        }
        
        private readonly Subject<SpriteEventData> _onPointerDown = new();
        private readonly Subject<SpriteEventData> _onPointerUp = new();
        private readonly Subject<SpriteEventData> _onPointerClick = new();
        private readonly Subject<SpriteEventData> _onPointerEnter = new();
        private readonly Subject<SpriteEventData> _onPointerLeave = new();

        public Observable<(Sprite2DApi api, Pointer2DData data)> OnPointerDownForBuddy(BuddyId buddyId) => ForBuddy(_onPointerDown, buddyId);
        public Observable<(Sprite2DApi api, Pointer2DData data)> OnPointerUpForBuddy(BuddyId buddyId) => ForBuddy(_onPointerUp, buddyId);
        public Observable<(Sprite2DApi api, Pointer2DData data)> OnPointerClickForBuddy(BuddyId buddyId) => ForBuddy(_onPointerClick, buddyId);
        public Observable<(Sprite2DApi api, Pointer2DData data)> OnPointerEnterForBuddy(BuddyId buddyId) => ForBuddy(_onPointerEnter, buddyId);
        public Observable<(Sprite2DApi api, Pointer2DData data)> OnPointerLeaveForBuddy(BuddyId buddyId) => ForBuddy(_onPointerLeave, buddyId);

        private Observable<(Sprite2DApi api, Pointer2DData data)> ForBuddy(Observable<SpriteEventData> src, BuddyId buddyId) => src
            .Where(item => item.BuddyId.Equals(buddyId))
            .Select(item => (item.Api, item.PointerData.ToApiValue()));
        
        public void InvokeOnPointerDown(Sprite2DApi api, Pointer2DDataInternal pointerData) 
            => _onPointerDown.OnNext(new SpriteEventData(api, pointerData));
        public void InvokeOnPointerUp(Sprite2DApi api, Pointer2DDataInternal pointerData)
            => _onPointerUp.OnNext(new SpriteEventData(api, pointerData));
        public void InvokeOnPointerClick(Sprite2DApi api, Pointer2DDataInternal pointerData)
            => _onPointerClick.OnNext(new SpriteEventData(api, pointerData));
        public void InvokeOnPointerEnter(Sprite2DApi api, Pointer2DDataInternal pointerData)
            => _onPointerEnter.OnNext(new SpriteEventData(api, pointerData));
        public void InvokeOnPointerLeave(Sprite2DApi api, Pointer2DDataInternal pointerData)
            => _onPointerLeave.OnNext(new SpriteEventData(api, pointerData));
    }
}
