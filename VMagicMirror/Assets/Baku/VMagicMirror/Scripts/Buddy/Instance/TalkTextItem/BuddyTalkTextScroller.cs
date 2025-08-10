using UnityEngine;
using R3;

namespace Baku.VMagicMirror.Buddy
{
    public class BuddyTalkTextScroller : MonoBehaviour
    {
        [SerializeField] private BuddyTalkTextScrollRect talkTextScrollRect;
        [SerializeField] private GameObject scrollBarObj;
        [SerializeField] private BuddyTalkTextScrollBarWatcher scrollBarWatcher;
        [SerializeField] private float bottomThreshold = 0.02f;

        private bool _shouldAutoScroll = true;
        private bool _upperScrollHappened = false;
        private bool IsScrollBarActive => scrollBarObj.activeSelf;

        /// <summary>
        /// <see cref="BuddyTalkTextInstance"/>が新しいテキストを表示し始めるときに呼び出すことで、
        /// 次のテキストに対して自動スクロールを有効にする
        /// </summary>
        public void ProceedToNextText()
        {
            _shouldAutoScroll = true;
            _upperScrollHappened = false;
        }

        private void Start()
        {
            talkTextScrollRect.onValueChanged
                .AddListener(OnScrollValueChanged);

            talkTextScrollRect.UserScrollHappened
                .Merge(scrollBarWatcher.UserScrollHappened)
                .Where(_ => IsScrollBarActive)
                .Subscribe(isUp =>
                {
                    _upperScrollHappened = isUp;
                    if (isUp)
                    {
                        _shouldAutoScroll = false;
                    }
                })
                .AddTo(this);
        }

        private void Update()
        {
            if (_shouldAutoScroll && IsScrollBarActive)
            {
                talkTextScrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private void OnScrollValueChanged(Vector2 position)
        {
            // このメソッドは「いちど中断された自動スクロールが(ユーザー入力での下スクロールで)再開する」というケースのみに着目する。
            // 「直近のスクロールが上向きでないかどうか」とかを判定しているのもそのため
            if (!_upperScrollHappened && position.y <= bottomThreshold　&& IsScrollBarActive)
            {
                _shouldAutoScroll = true;
            }
        }
    }
}