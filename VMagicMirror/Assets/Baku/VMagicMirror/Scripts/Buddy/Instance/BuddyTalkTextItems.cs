
//NOTE: このファイルは ITalkText の内部実装になっているが、イベントの発火制御のために少しI/Fのプロパティが増えていたりする
namespace Baku.VMagicMirror.Buddy
{
    public enum TalkTextItemTypeInternal
    {
        Text,
        Wait,
    }
    
    public interface ITalkTextItemInternal
    {
        TalkTextApi Api { get; }
        BuddyId BuddyId { get; }
        TalkTextItemTypeInternal Type { get; }
        string Key { get; }
        string Text { get; }
        float ScheduledDuration { get; }
        
        ITalkTextItemInternal WithApi(TalkTextApi api);
    }

    public readonly struct TextTalkItemInternal : ITalkTextItemInternal
    {
        public TextTalkItemInternal(TalkTextApi api, BuddyId buddyId, string text, string key, float duration)
        {
            Api = api;
            BuddyId = buddyId;
            Text = text;
            Key = key;
            ScheduledDuration = duration;
        }
        
        public TalkTextApi Api { get; }
        public BuddyId BuddyId { get; }

        public TalkTextItemTypeInternal Type => TalkTextItemTypeInternal.Text;
        public string Text { get; }
        public string Key { get; }
        public float ScheduledDuration { get; }
        
        public ITalkTextItemInternal WithApi(TalkTextApi api) 
            => new TextTalkItemInternal(api, BuddyId, Text, Key, ScheduledDuration);
    }
    
    public readonly struct WaitTalkItemInternal : ITalkTextItemInternal
    {
        public WaitTalkItemInternal(TalkTextApi api, BuddyId buddyId, string key, float duration)
        {
            Api = api;
            BuddyId = buddyId;
            Key = key;
            ScheduledDuration = duration;
        }

        public TalkTextApi Api { get; }
        public BuddyId BuddyId { get; }
        public TalkTextItemTypeInternal Type => TalkTextItemTypeInternal.Wait;
        public string Text => "";
        public string Key { get; }
        public float ScheduledDuration { get; }
        
        public ITalkTextItemInternal WithApi(TalkTextApi api) 
            => new WaitTalkItemInternal(api, BuddyId, Key, ScheduledDuration);
    }
    
}