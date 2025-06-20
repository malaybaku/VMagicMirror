using System;
using Baku.VMagicMirror.Buddy.Api;
using UniRx;

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

        public IObservable<SpriteEventData> OnPointerDown => _onPointerDown;
        public IObservable<SpriteEventData> OnPointerUp => _onPointerUp;
        public IObservable<SpriteEventData> OnPointerClick => _onPointerClick;
        public IObservable<SpriteEventData> OnPointerEnter => _onPointerEnter;
        public IObservable<SpriteEventData> OnPointerLeave => _onPointerLeave;
        
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
