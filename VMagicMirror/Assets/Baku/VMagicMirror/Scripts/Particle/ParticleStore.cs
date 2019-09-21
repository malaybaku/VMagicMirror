using System;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public class ParticleStore : MonoBehaviour
    {
        public const int InvalidTypingEffectIndex = -1;

        [Serializable]
        struct ParticlePrefabInfo
        {
            public Transform prefab;
            public Vector3 scale;
            public bool useCollisionPlane;
            public Transform collisionTransform;
        }

        [SerializeField]
        [Tooltip("同時に表示するエフェクト数の上限。増やすと表示が破綻しにくくなるかわりメモリとCPU負荷が増える。")]
        private int _particleStoreCount = 16;

        [SerializeField]
        private ParticlePrefabInfo[] _particlePrefabs = null;

        public Vector3 ParticleScale { get; set; } = new Vector3(0.7f, 1.0f, 0.7f);

        int _nextParticleIndex = 0;

        //-1はパーティクル無効、0~(particlePrefabs.Length - 1)は有効な状態を表す
        private int _currentSelectedParticlePrefabIndex = InvalidTypingEffectIndex;

        //キャッシュして多数同時に実行できるようにしたパーティクル群
        private ParticleSystem[] _particles = new ParticleSystem[0];
        
        //-1を指定したらパーティクル無し、0以上で配列内を指定したら有効なパーティクルで初期化
        public void SetParticleIndex(int index)
        {
            if (_currentSelectedParticlePrefabIndex == index)
            {
                return;
            }

            _currentSelectedParticlePrefabIndex = index;

            for (int i = 0; i < _particles.Length; i++)
            {
                Destroy(_particles[i].gameObject);
            }

            //パーティクルを無効化したい場合はコレで終わり
            if (index < 0 || index >= _particlePrefabs.Length)
            {
                _particles = new ParticleSystem[0];
                return;
            }

            var prefabSource = _particlePrefabs[index];

            //パーティクルを有効化する場合
            _particles = new ParticleSystem[_particleStoreCount];
            for (int i = 0; i < _particles.Length; i++)
            {
                _particles[i] = Instantiate(prefabSource.prefab, this.transform).GetComponent<ParticleSystem>();
                if (prefabSource.useCollisionPlane)
                {
                    _particles[i].collision.SetPlane(0, prefabSource.collisionTransform);
                }

                //NOTE: ここではパーティクルの基準サイズのみを適用し、実行段階でキーボードのサイズに即したスケーリングを追加的に調整。
                this.transform.localScale = prefabSource.scale;
            }
        }

        public void RequestParticleStart(Vector3 worldPosition)
        {
            if (_particles.Length == 0)
            {
                return;
            }

            //パーティクルの切り替えタイミングによっては配列外参照するリスクがあるのでその対策
            if (_nextParticleIndex >= _particles.Length)
            {
                _nextParticleIndex = 0;
            }

            var particle = _particles[_nextParticleIndex];
            if (particle.isPlaying)
            {
                particle.Stop();
            }

            particle.transform.position = worldPosition;
            particle.transform.localScale = ParticleScale;
            particle.Play();

            _nextParticleIndex++;
            if (_nextParticleIndex >= _particles.Length)
            {
                _nextParticleIndex = 0;
            }
        }
    }
}
