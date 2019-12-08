using System;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public class ParticleStore : MonoBehaviour
    {
        public const int InvalidTypingEffectIndex = -1;

        [Serializable]
        class ParticlePrefabInfo
        {
            public Transform prefab = null;
            public Vector3 scale = Vector3.one;
            public bool useCollisionPlane = false;
            public Transform collisionTransform = null;
        }

        //NOTE: マウスのprefabはボタンが1つ以上クリックされているとき、インデックスに対応して発火します。
        [Serializable]
        class MouseParticlePrefabInfo
        {
            //マウスボタンが押されている間はPlayされるやつ
            public ParticleSystem continueParticlePrefab;
            //マウスのボタンを離した瞬間に一度さけPlayされるやつ
            public ParticleSystem clickParticlePrefab;
        }

        [Tooltip("同時に表示するエフェクト数の上限。増やすと表示が破綻しにくくなるかわりメモリとCPU負荷が増える。")]
        [SerializeField] private int particleStoreCount = 16;
        [SerializeField] private ParticlePrefabInfo[] particlePrefabs = null;

        [SerializeField] private MouseParticlePrefabInfo[] mouseParticlePrefabs = null;
        [SerializeField] private Transform mouseParticlePrefabParent = null;

        //NOTE: この秒数だけPlayMouseParticleが呼ばれなかった場合、マウスが止まっているのでパーティクルを止める
        [SerializeField] private float mouseContinueParticleCount = 0.5f;
        
        public Vector3 ParticleScale { get; set; } = new Vector3(0.7f, 1.0f, 0.7f);

        private int _nextParticleIndex = 0;
        //-1はパーティクル無効、0~(particlePrefabs.Length - 1)は有効な状態を表す
        private int _currentSelectedParticlePrefabIndex = InvalidTypingEffectIndex;
        //キャッシュして多数同時に実行できるようにしたパーティクル群
        private ParticleSystem[] _particles = new ParticleSystem[0];
        //マウスのパーティクル
        private ParticleSystem _mouseContinueParticle = null;
        private ParticleSystem _mouseEndParticle = null;
        private float _mouseContinueParticleCountDown = 0f;

        private void Update()
        {
            if (_mouseContinueParticleCountDown <= 0)
            {
                return;
            }
            
            _mouseContinueParticleCountDown -= Time.deltaTime;
            if (_mouseContinueParticleCountDown <= 0 && 
                _mouseContinueParticle != null &&
                _mouseContinueParticle.isPlaying)
            {
                _mouseContinueParticle.Stop();
            }
        }
        
        //-1を指定したらパーティクル無し、0以上で配列内を指定したら有効なパーティクルで初期化
        public void SetParticleIndex(int index)
        {
            if (_currentSelectedParticlePrefabIndex == index)
            {
                return;
            }

            _currentSelectedParticlePrefabIndex = index;

            ClearParticles();
            
            //パーティクルを無効化したい場合はコレで終わり
            if (index < 0 || index >= particlePrefabs.Length)
            {
                return;
            }
            
            SetupKeyboardParticle(index);
            SetupMouseParticle(index);
        }

        //指定した場所でキーボードパーティクルを起動します。
        public void RequestKeyboardParticleStart(Vector3 worldPosition)
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

        /// <summary>
        /// マウスが移動したときのパーティクル実行をリクエストします。
        /// </summary>
        /// <param name="worldPosition"></param>
        public void RequestMouseMoveParticle(Vector3 worldPosition)
        {
            if (_mouseContinueParticle == null || 
                _mouseEndParticle == null)
            {
                return;
            }
            
            _mouseContinueParticle.transform.position = worldPosition;
            _mouseEndParticle.transform.position = worldPosition;
            if (!_mouseContinueParticle.isPlaying)
            {
                _mouseContinueParticle.Play();
            }
            _mouseContinueParticleCountDown = mouseContinueParticleCount;
        }

        /// <summary>
        /// マウスをクリックしたときのパーティクル実行をリクエストします。
        /// </summary>
        public void RequestMouseClickParticle()
        {
            if (_mouseEndParticle != null)
            {
                _mouseEndParticle.Play();
            }
        }
        
        private void ClearParticles()
        {
            for (int i = 0; i < _particles.Length; i++)
            {
                Destroy(_particles[i].gameObject);
            }
            _particles = new ParticleSystem[0];

            if (_mouseContinueParticle != null)
            {
                Destroy(_mouseContinueParticle.gameObject);
                _mouseContinueParticle = null;
            }

            if (_mouseEndParticle != null)
            {
                Destroy(_mouseEndParticle.gameObject);
                _mouseEndParticle = null;
            }
        }
        
        private void SetupKeyboardParticle(int index)
        {
            var prefabSource = particlePrefabs[index];

            //パーティクルを有効化する場合
            _particles = new ParticleSystem[particleStoreCount];
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

        private void SetupMouseParticle(int index)
        {
            _mouseContinueParticle = Instantiate(
                mouseParticlePrefabs[index].continueParticlePrefab, 
                mouseParticlePrefabParent
            );
            _mouseContinueParticle.transform.localPosition = Vector3.zero;
            _mouseContinueParticle.transform.localRotation = Quaternion.identity;

            _mouseEndParticle = Instantiate(
                mouseParticlePrefabs[index].clickParticlePrefab,
                mouseParticlePrefabParent
            );
            _mouseEndParticle.transform.localPosition = Vector3.zero;
            _mouseEndParticle.transform.localRotation = Quaternion.identity;
        }
        

    }
}
