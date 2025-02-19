using UnityEngine;

namespace Baku.VMagicMirror.Buddy
{
    public class BuddyAudioSources : MonoBehaviour
    {
        private const int InstanceCount = 10;
        [SerializeField] private AudioSource audioSourcePrefab;

        private readonly AudioSource[] _audioSources = new AudioSource[InstanceCount];
        private bool _initialized;
        private int _index = 0;
        private void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            for (var i = 0; i < InstanceCount; i++)
            {
                _audioSources[i] = Instantiate(audioSourcePrefab, transform);
            }
            _initialized = true;
        }
        
        public AudioSource GetAudioSource()
        {
            Initialize();

            var result = _audioSources[_index];
            _index = (_index + 1) % InstanceCount;
            return result;
        }
    }
}

