
//NOTE: このファイルは ITalkText の内部実装になっていて、
namespace Baku.VMagicMirror.Buddy
{
    public enum TalkTextItemTypeInternal
    {
        Text,
        Wait,
    }
    
    public interface ITalkTextItemInternal
    {
        BuddyId BuddyId { get; }
        TalkTextItemTypeInternal Type { get; }
        string Key { get; }
        string Text { get; }
        float ScheduledDuration { get; }
    }

    public readonly struct TextTalkItemInternal : ITalkTextItemInternal
    {
        public TextTalkItemInternal(BuddyId buddyId, string text, string key, float duration)
        {
            BuddyId = buddyId;
            Text = text;
            Key = key;
            ScheduledDuration = duration;
        }

        public BuddyId BuddyId { get; }

        public TalkTextItemTypeInternal Type => TalkTextItemTypeInternal.Text;
        public string Text { get; }
        public string Key { get; }
        public float ScheduledDuration { get; }
    }
    
    public readonly struct WaitTalkItemInternal : ITalkTextItemInternal
    {
        public WaitTalkItemInternal(BuddyId buddyId, string key, float duration)
        {
            BuddyId = buddyId;
            Key = key;
            ScheduledDuration = duration;
        }

        public BuddyId BuddyId { get; }
        public TalkTextItemTypeInternal Type => TalkTextItemTypeInternal.Wait;
        public string Text => "";
        public string Key { get; }
        public float ScheduledDuration { get; }
    }
    
}