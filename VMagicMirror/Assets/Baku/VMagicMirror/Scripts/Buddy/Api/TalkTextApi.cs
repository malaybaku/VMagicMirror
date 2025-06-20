using System;
using UniRx;
using VMagicMirror.Buddy;

namespace Baku.VMagicMirror.Buddy
{
    public class TalkTextApi : ITalkText
    {
        // NOTE: Buddyを跨いで同一のカウンターを使ってよい (とくに害もないので)
        private static int _keyCounter = 0;
        private static readonly object KeyCounterLock = new();
        private static int GetNextKey()
        {
            lock (KeyCounterLock)
            {
                if (_keyCounter >= int.MaxValue)
                {
                    _keyCounter = 0;
                }
                return _keyCounter++;
            }
        }

        public TalkTextApi(BuddyTalkTextInstance instance, BuddyTalkTextEventBroker eventBroker)
        {
            _instance = instance;
            _eventBroker = eventBroker;
            SubscribeInstance();
        }

        private readonly BuddyTalkTextInstance _instance;
        private readonly BuddyTalkTextEventBroker _eventBroker;

        private void SubscribeInstance()
        {
            
            _instance.ItemDequeued
                .Subscribe(item => _eventBroker.DequeueItem(item.WithApi(this)))
                .AddTo(_instance);
            _instance.ItemFinished
                .Subscribe(item => _eventBroker.FinishItem(item.WithApi(this)))
                .AddTo(_instance);
        }

        public event Action<ITalkTextItem> ItemDequeued;
        public event Action<ITalkTextItem> ItemFinished;
        
        internal void OnItemDequeued(ITalkTextItem item) => ItemDequeued?.Invoke(item);
        internal void OnItemFinished(ITalkTextItem item) => ItemFinished?.Invoke(item);

        public ITalkTextItem GetCurrentPlayingItem()
        {
            if (_instance.QueuedItems.Count == 0)
            {
                return null;
            }
            
            var item = _instance.QueuedItems[0];
            return item.Type switch
            {
                TalkTextItemTypeInternal.Text => new TextTalkItem(item.Text, item.Key, item.ScheduledDuration),
                TalkTextItemTypeInternal.Wait => new WaitTalkItem(item.Key, item.ScheduledDuration),
                _ => null,
            };
        }

        public int QueueCount => _instance.QueuedItems.Count;
        
        public string ShowText(string text, float speed = 10, float waitAfterCompleted = 8, string key = "")
        {
            if (string.IsNullOrEmpty(text))
            {
                return "";
            }

            var actualKey = string.IsNullOrEmpty(key) ? GetNextKey().ToString() : key;
            var textDuration = speed <= 0 ? 0f : text.Length / speed;
            _instance.AddTalkItem(text, actualKey, textDuration);

            // NOTE: この経路で待ち処理を追加するとき、待ちに対してのキーは割当らない(これはAPI仕様)
            if (waitAfterCompleted > 0)
            {
                _instance.AddWaitItem("", waitAfterCompleted);
            }
            
            return actualKey;
        }

        public string Wait(float duration, string key = "")
        {
            if (duration <= 0)
            {
                return "";
            }

            var actualKey = string.IsNullOrEmpty(key) ? GetNextKey().ToString() : key;
            _instance.AddWaitItem(actualKey, duration);
            return actualKey;
        }

        public void Clear(bool includeCurrentItem = true) => _instance.Clear(includeCurrentItem);
    }


    public readonly struct TextTalkItem : ITalkTextItem
    {
        public TextTalkItem(string text, string key, float duration)
        {
            Text = text;
            Key = key;
            ScheduledDuration = duration;
        }

        public TalkTextItemType Type => TalkTextItemType.Text;
        public string Text { get; }
        public string Key { get; }
        public float ScheduledDuration { get; }
    }
    
    public readonly struct WaitTalkItem : ITalkTextItem
    {
        public WaitTalkItem(string key, float duration)
        {
            Key = key;
            ScheduledDuration = duration;
        }

        public TalkTextItemType Type => TalkTextItemType.Wait;
        public string Text => "";
        public string Key { get; }
        public float ScheduledDuration { get; }
    }
}
