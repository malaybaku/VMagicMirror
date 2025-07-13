
//NOTE: このファイルは ITalkText の内部実装になっているが、イベントの発火制御のために少しI/Fのプロパティが増えていたりする
namespace Baku.VMagicMirror.Buddy
{
    public enum TalkTextItemTypeInternal
    {
        Text,
        Wait,
    }
    
    public readonly struct TalkTextItemInternal
    {
        public TalkTextApi Api { get; }
        public BuddyId BuddyId { get; }
        public TalkTextItemTypeInternal Type { get; }
        public string Key { get; }
        public string Text { get; }
        public float ScheduledDuration { get; }

        public TalkTextItemInternal(
            TalkTextApi api,
            BuddyId buddyId,
            TalkTextItemTypeInternal type,
            string key,
            string text,
            float scheduledDuration
            )
        {
            Api = api;
            BuddyId = buddyId;
            Type = type;
            Key = key;
            Text = text;
            ScheduledDuration = scheduledDuration;
        }

        public static TalkTextItemInternal CreateText(BuddyId buddyId, string text, string key, float duration)
            => new(null, buddyId, TalkTextItemTypeInternal.Text, key, text, duration);

        public static TalkTextItemInternal CreateWait(BuddyId buddyId, string key, float duration)
            => new(null, buddyId, TalkTextItemTypeInternal.Wait, key, "", duration);
        
        public TalkTextItemInternal WithApi(TalkTextApi api) => new(
            api,
            BuddyId,
            Type,
            Key,
            Text,
            ScheduledDuration
        );
    }
}