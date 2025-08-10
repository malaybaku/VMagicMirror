using System;
using R3;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Baku.VMagicMirror.Buddy
{
    public class BuddyTalkTextScrollBarWatcher : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [SerializeField] Scrollbar targetScrollbar;
        
        private readonly Subject<bool> _userScrollHappenedHappened = new();
        public Observable<bool> UserScrollHappened => _userScrollHappenedHappened;

        private bool _isDragging;
        private float _lastValue;

        private void Awake()
        {
            _lastValue = targetScrollbar.value;
            targetScrollbar.onValueChanged.AddListener(OnValueChanged);
        }

        public void OnPointerDown(PointerEventData e)
        {
            _isDragging = true;
            _lastValue = targetScrollbar.value;
        }

        // ドラッグ中は ValueChanged イベントでケアする
        public void OnDrag(PointerEventData e)
        {
        }

        public void OnPointerUp(PointerEventData e) => _isDragging = false;

        private void OnValueChanged(float newValue)
        {
            // プログラム変更なら無視
            if (!_isDragging) return; 
            
            // とくにスクロールの端付近で発生するような、ほとんど動きとして認識できないような差についても無視
            if (Mathf.Abs(newValue - _lastValue) < 0.0001f) return;

            var isUp = newValue > _lastValue;
            _lastValue = newValue;
            _userScrollHappenedHappened.OnNext(isUp);
        }
    }
}