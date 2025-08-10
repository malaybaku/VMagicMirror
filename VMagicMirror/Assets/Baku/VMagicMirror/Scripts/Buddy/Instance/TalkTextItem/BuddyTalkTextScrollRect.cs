using System;
using R3;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Baku.VMagicMirror.Buddy
{
    /// <summary>
    /// テキストの自動スクロールを止めたり再開したりするために、
    /// 「ユーザー入力でスクロールが変化した」というイベントが検出できるように拡張したScrollRectクラス
    /// </summary>
    public class BuddyTalkTextScrollRect : ScrollRect
    {
        private Vector2 _lastDragPos;

        private readonly Subject<bool> _userScrollHappened = new();
        public IObservable<bool> UserScrollHappened => _userScrollHappened;
        private void InvokeUserScroll(bool isUp) => _userScrollHappened.OnNext(isUp);

        public override void OnScroll(PointerEventData data)
        {
            base.OnScroll(data);
            if (vertical && data.scrollDelta.y != 0f)
            {
                // scrollDelta.y > 0 でホイール上スクロール
                var isUp = data.scrollDelta.y > 0f;
                InvokeUserScroll(isUp);
            }
        }
    }    
}
