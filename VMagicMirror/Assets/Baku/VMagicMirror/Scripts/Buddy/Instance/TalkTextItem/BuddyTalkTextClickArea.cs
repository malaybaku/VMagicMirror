using System;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Baku.VMagicMirror.Buddy
{
    public class BuddyTalkTextClickArea : MonoBehaviour, IPointerClickHandler
    {
        private readonly Subject<Unit> _clicked = new();
        public IObservable<Unit> Clicked => _clicked;
        
        void IPointerClickHandler.OnPointerClick(PointerEventData eventData) => _clicked.OnNext(Unit.Default);
    }
}