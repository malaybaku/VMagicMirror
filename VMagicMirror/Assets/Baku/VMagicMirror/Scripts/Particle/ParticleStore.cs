using UnityEngine;

namespace Baku.VMagicMirror
{
    public class ParticleStore : MonoBehaviour
    {
        [SerializeField]
        private ParticleSystem[] _particleSources = new ParticleSystem[0];

        int _nextParticleIndex = 0;

        public Vector3 ParticleScale { get; set; } = new Vector3(0.7f, 1, 0.7f);

        public void RequestParticleStart(Vector3 worldPosition)
        {
            var particle = _particleSources[_nextParticleIndex];
            if (particle.isPlaying)
            {
                particle.Stop();
            }

            particle.transform.position = worldPosition;
            particle.transform.localScale = ParticleScale;
            particle.Play();

            _nextParticleIndex++;
            if (_nextParticleIndex >= _particleSources.Length)
            {
                _nextParticleIndex = 0;
            }
        }

    }
}
