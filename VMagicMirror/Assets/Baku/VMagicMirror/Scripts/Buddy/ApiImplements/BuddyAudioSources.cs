using System;
using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror.Buddy
{
    public class BuddyAudioSources : MonoBehaviour
    {
        readonly struct AudioPlayState
        {
            public readonly bool IsPlaying;
            public readonly BuddyId Id;
            public readonly string Key;
            
            public AudioPlayState(bool isPlaying, BuddyId id, string key)
            {
                IsPlaying = isPlaying;
                Id = id;
                Key = key;
            }

            public static AudioPlayState Empty { get; } = new(false, BuddyId.Empty, "");
        }
        
        private const int InstanceCount = 10;
        [SerializeField] private AudioSource audioSourcePrefab;

        private readonly AudioSource[] _audioSources = new AudioSource[InstanceCount];
        private readonly AudioPlayState[] _audioPlayStates = new AudioPlayState[InstanceCount];
        private bool _initialized;
        private int _index = 0;

        // NOTE: Stopで再生停止した音声のキー情報を返却するときに使うやつだよ
        private readonly string[] _keys = new string[InstanceCount];
        
        private readonly Subject<(BuddyId, string)> _audioCompleted = new();
        public IObservable<(BuddyId id, string key)> AudioCompleted => _audioCompleted;

        private readonly Subject<(BuddyId, string)> _audioInterrupted = new();
        public IObservable<(BuddyId id, string key)> AudioInterrupted => _audioInterrupted;

        private void Update()
        {
            if (!_initialized) return;
        
            // 「自然に isPlaying が false になる」というのをチェックしている
            for (var i = 0; i < _audioSources.Length; i++)
            {
                var audioSource = _audioSources[i];
                var state = _audioPlayStates[i];
                if (state.IsPlaying && !audioSource.isPlaying)
                {
                    _audioPlayStates[i] = AudioPlayState.Empty;
                    _audioCompleted.OnNext((state.Id, state.Key));
                }
            }
        }
        
        private void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            for (var i = 0; i < InstanceCount; i++)
            {
                _audioSources[i] = Instantiate(audioSourcePrefab, transform);
                _audioPlayStates[i] = AudioPlayState.Empty;
            }
            _initialized = true;
        }
        
        public AudioSource GetAudioSource()
        {
            Initialize();

            var result = _audioSources[_index];
            
            // 再生中のAudioSourceがぶんどられた == そのAudioSourceをPlayするはずなので、既存の音源の再生中断するのが確定
            if (_audioPlayStates[_index].IsPlaying)
            {
                var state = _audioPlayStates[_index];
                _audioPlayStates[_index] = AudioPlayState.Empty;
                _audioInterrupted.OnNext((state.Id, state.Key));
            }

            _index = (_index + 1) % InstanceCount;
            return result;
        }

        // NOTE: AudioSourceの再生処理を記述した呼び出し元が、 GetAudioSource() の後で呼び出す
        public void MarkAsPlaying(AudioSource source, BuddyId id, string key)
        {
            for (var i = 0; i < _audioSources.Length; i++)
            {
                if (_audioSources[i] == source)
                {
                    _audioPlayStates[i] = new AudioPlayState(true, id, key);
                    return;
                }
            }
        }
        
        // NOTE: 2引数版と異なり、BuddyIdさえ一致してれば停止させる
        public Span<string> Stop(BuddyId buddyId)
        {
            var count = 0;
            for (var i = 0; i < _audioSources.Length; i++)
            {
                var state = _audioPlayStates[i];
                if (state.IsPlaying && state.Id.Equals(buddyId))
                {
                    _audioPlayStates[i] = AudioPlayState.Empty;
                    _keys[i] = "";
                    count++;
                }
            }
            return _keys.AsSpan(0, count);
        }

        public Span<string> Stop(BuddyId buddyId, string key)
        {
            var count = 0;
            for (var i = 0; i < _audioSources.Length; i++)
            {
                var state = _audioPlayStates[i];
                if (state.IsPlaying && state.Id.Equals(buddyId) && state.Key == key)
                {
                    _audioPlayStates[i] = AudioPlayState.Empty;
                    _keys[i] = key;
                    count++;
                }
            }
            return _keys.AsSpan(0, count);
        }
    }
}

