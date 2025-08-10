using System;
using R3;
using VMagicMirror.Buddy;

namespace Baku.VMagicMirror.Buddy
{
    public class BuddyAudioEventBroker
    {
        private readonly Subject<(BuddyId id, AudioStartedInfo info)> _audioStarted = new();
        private readonly Subject<(BuddyId id, AudioStoppedInfo info)> _audioStopped = new();
        
        public IObservable<AudioStartedInfo> AudioStartedForBuddy(BuddyId id)
            => _audioStarted.Where(x => x.id.Equals(id)).Select(x => x.info);

        public IObservable<AudioStoppedInfo> AudioStoppedForBuddy(BuddyId id)
            => _audioStopped.Where(x => x.id.Equals(id)).Select(x => x.info);
        
        public void InvokeAudioStarted(BuddyId id, string key, float length)
            => _audioStarted.OnNext((id, new AudioStartedInfo(key, length)));

        public void InvokeAudioStopped(BuddyId id, string key, InternalAudioStoppedReason reason)
        {
            var infoReason = reason switch 
            {
                InternalAudioStoppedReason.Completed => AudioStoppedReason.Completed,
                InternalAudioStoppedReason.Stopped => AudioStoppedReason.Stopped,
                InternalAudioStoppedReason.Interrupted => AudioStoppedReason.Interrupted,
                _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null)
            };
            _audioStopped.OnNext((id, new AudioStoppedInfo(key, infoReason)));
        }
    }

    // NOTE: Invoke~ 関数を使うクラス側でI/Fのnamespaceを見に行かないで済むようにするのを優先し、enumを二重定義してる
    public enum InternalAudioStoppedReason
    {
        Completed,
        Stopped,
        Interrupted,
    }
}
