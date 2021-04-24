using System;
using Baku.VMagicMirror.Installer;
using UnityEngine;
using Zenject;

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
            //NOTE: コレは非nullにする場合InjectしたKeyboardなりMidiControllerなりを使うため、プロパティでよい(serializableじゃなくていい)
            public Transform CollisionTransform { get; set; }= null;
        }

        //NOTE: マウスのprefabはボタンが1つ以上クリックされているとき、インデックスに対応して発火します。
        [Serializable]
        class MouseParticlePrefabInfo
        {
#pragma warning disable CS0649            
            //マウスボタンが押されている間はPlayされるやつ
            public ParticleSystem continueParticlePrefab;
            //マウスのボタンを離した瞬間に一度さけPlayされるやつ
            public ParticleSystem clickParticlePrefab;
#pragma warning restore CS0649
        }

        [Tooltip("同時に表示するエフェクト数の上限。増やすと表示が破綻しにくくなるかわりメモリとCPU負荷が増える。")]
        [SerializeField] private int particleStoreCount = 16;
        [SerializeField] private ParticlePrefabInfo[] particlePrefabs = null;
        [SerializeField] private ParticlePrefabInfo[] midiParticlePrefabs = null;
        [SerializeField] private MouseParticlePrefabInfo[] mouseParticlePrefabs = null;

        //NOTE: この秒数だけPlayMouseParticleが呼ばれなかった場合、マウスが止まっているのでパーティクルを止める
        [SerializeField] private float mouseContinueParticleCount = 0.5f;
        
        public Vector3 KeyboardParticleScale { get; set; } = new Vector3(0.7f, 1.0f, 0.7f);
        public Quaternion KeyboardParticleRotation { get; set; } = Quaternion.identity;
        public Vector3 MidiParticleScale { get; set; } = new Vector3(0.7f, 1.0f, 0.7f);
        public Quaternion MidiParticleRotation { get; set; } = Quaternion.identity;
        
        private Transform _keyboardParticleParent = null;
        private Transform _mouseParticlePrefabParent = null;

        private int _nextKeyboardParticleIndex = 0;
        private int _nextArcadeStickParticleIndex = 0;
        private int _nextMidiParticleIndex = 0;
        //-1はパーティクル無効、0~(particlePrefabs.Length - 1)は有効な状態を表す
        private int _keyAndPadParticleTypeIndex = InvalidTypingEffectIndex;
        private int _midiParticleTypeIndex = InvalidTypingEffectIndex;
        private int _arcadeStickParticleTypeIndex = InvalidTypingEffectIndex;
        
        //キャッシュして多数同時に実行できるようにしたパーティクル群
        private ParticleSystem[] _keyParticles = new ParticleSystem[0];
        private ParticleSystem[] _arcadeStickParticles = new ParticleSystem[0];
        private ParticleSystem[] _midiParticles = new ParticleSystem[0];
        //マウスのパーティクル
        private ParticleSystem _mouseContinueParticle = null;
        private ParticleSystem _mouseEndParticle = null;
        private float _mouseContinueParticleCountDown = 0f;

        //ペンタブのパーティクル: マウスとほとんど同じことをやるが
        private ParticleSystem _penTabletContinueParticle = null;
        private ParticleSystem _penTabletEndParticle = null;
        private float _penTabletContinueParticleCountDown = 0f;

        [Inject]
        public void Initialize(
            IMessageReceiver receiver,
            IDevicesRoot devicesRoot, 
            KeyboardProvider keyboard,
            TouchPadProvider touchPad,
            PenTabletProvider penTablet)
        {
            transform.parent = devicesRoot.Transform;
            _keyboardParticleParent = keyboard.transform;
            _mouseParticlePrefabParent = touchPad.transform;
            
            var _ = new ParticleControlReceiver(receiver, this);
        }

        private void Start()
        {
            for (int i = 0; i < particlePrefabs.Length; i++)
            {
                particlePrefabs[i].CollisionTransform = _keyboardParticleParent;
            }
        }

        private void Update()
        {
            UpdateMouse();
            UpdatePenTablet();

            void UpdateMouse()
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

            void UpdatePenTablet()
            {
                if (_penTabletContinueParticleCountDown <= 0)
                {
                    return;
                }
            
                _penTabletContinueParticleCountDown -= Time.deltaTime;
                if (_penTabletContinueParticleCountDown <= 0 && 
                    _penTabletContinueParticle != null &&
                    _penTabletContinueParticle.isPlaying)
                {
                    _penTabletContinueParticle.Stop();
                }
            }
        }
        
        //-1を指定したらパーティクル無し、0以上で配列内を指定したら有効なパーティクルで初期化
        public void SetParticleIndex(int keyAndPadIndex, int midiIndex, int arcadeStickIndex)
        {
            if (_keyAndPadParticleTypeIndex != keyAndPadIndex)
            {
                _keyAndPadParticleTypeIndex = keyAndPadIndex;
                ClearKeyAndPadParticles();
                ClearPenTabletParticle();
                if (keyAndPadIndex >= 0 && keyAndPadIndex < particlePrefabs.Length)
                {
                    SetupKeyboardParticle(keyAndPadIndex);
                    SetupMouseParticle(keyAndPadIndex);
                    SetupPenTabletParticle(keyAndPadIndex);
                }
            }

            if (_midiParticleTypeIndex != midiIndex)
            {
                _midiParticleTypeIndex = midiIndex;
                ClearMidiParticles();
                if (midiIndex >= 0 && midiIndex < midiParticlePrefabs.Length)
                {
                    SetupMidiParticle(midiIndex);
                }
            }

            if (_arcadeStickParticleTypeIndex != arcadeStickIndex)
            {
                _arcadeStickParticleTypeIndex = arcadeStickIndex;
                ClearArcadeStickParticles();
                if (arcadeStickIndex >= 0 && arcadeStickIndex < particlePrefabs.Length)
                {
                    SetupArcadeStickParticle(arcadeStickIndex);
                }
            }
            
            
        }

        /// <summary>
        /// 指定した場所でキーボードパーティクルを起動します。
        /// </summary>
        /// <param name="worldPosition"></param>
        public void RequestKeyboardParticleStart(Vector3 worldPosition)
        {
            if (_keyParticles.Length == 0)
            {
                return;
            }

            //パーティクルの切り替えタイミングによっては配列外参照するリスクがあるのでその対策
            if (_nextKeyboardParticleIndex >= _keyParticles.Length)
            {
                _nextKeyboardParticleIndex = 0;
            }

            var particle = _keyParticles[_nextKeyboardParticleIndex];
            if (particle.isPlaying)
            {
                particle.Stop();
            }

            var t = particle.transform;
            t.position = worldPosition;
            t.localRotation = KeyboardParticleRotation;
            t.localScale = KeyboardParticleScale;
            particle.Play();

            _nextKeyboardParticleIndex++;
            if (_nextKeyboardParticleIndex >= _keyParticles.Length)
            {
                _nextKeyboardParticleIndex = 0;
            }
        }

        /// <summary>
        /// 指定した位置でアケコン用のパーティクルを起動します。
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <param name="worldRotation"></param>
        public void RequestArcadeStickParticleStart(Vector3 worldPosition, Quaternion worldRotation)
        {
            if (_arcadeStickParticles.Length == 0)
            {
                return;
            }

            //パーティクルの切り替えタイミングによっては配列外参照するリスクがあるのでその対策
            if (_nextArcadeStickParticleIndex >= _arcadeStickParticles.Length)
            {
                _nextArcadeStickParticleIndex = 0;
            }

            var particle = _arcadeStickParticles[_nextArcadeStickParticleIndex];
            if (particle.isPlaying)
            {
                particle.Stop();
            }

            var t = particle.transform;
            t.position = worldPosition;
            t.localRotation = worldRotation;
            //NOTE: アケコンはスケール不変だからoneで問題ないはず
            t.localScale = Vector3.one;
            particle.Play();

            _nextArcadeStickParticleIndex++;
            if (_nextArcadeStickParticleIndex >= _arcadeStickParticles.Length)
            {
                _nextArcadeStickParticleIndex = 0;
            }
        }

        /// <summary>
        /// 指定した場所でMIDIコントローラパーティクルを起動します。
        /// </summary>
        /// <param name="worldPosition"></param>
        public void RequestMidiParticleStart(Vector3 worldPosition)
        {
            if (_midiParticles.Length == 0)
            {
                return;
            }

            //パーティクルの切り替えタイミングによっては配列外参照するリスクがあるのでその対策
            if (_nextMidiParticleIndex >= _midiParticles.Length)
            {
                _nextMidiParticleIndex = 0;
            }

            var particle = _midiParticles[_nextMidiParticleIndex];
            if (particle.isPlaying)
            {
                particle.Stop();
            }

            var t = particle.transform;
            t.position = worldPosition;
            t.localRotation = MidiParticleRotation;
            t.localScale = MidiParticleScale;
            particle.Play();

            _nextMidiParticleIndex++;
            if (_nextMidiParticleIndex >= _midiParticles.Length)
            {
                _nextMidiParticleIndex = 0;
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

        /// <summary>
        /// ペンタブのペンが移動したときのパーティクル実行をリクエストします。
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        public void RequestPenTabletMoveParticle(Vector3 position, Quaternion rotation)
        {
            if (_penTabletContinueParticle == null ||
                _penTabletEndParticle == null)
            {
                return;
            }

            _penTabletContinueParticle.transform.SetPositionAndRotation(position, rotation);
            _penTabletEndParticle.transform.SetPositionAndRotation(position, rotation);
            if (!_penTabletContinueParticle.isPlaying)
            {
                _penTabletContinueParticle.Play();
            }

            _penTabletContinueParticleCountDown = mouseContinueParticleCount;
        }

        /// <summary>
        /// ペンタブモードでクリックしたときのパーティクル実行をリクエストします。
        /// </summary>
        public void RequestPenTabletClickParticle()
        {
            if (_penTabletEndParticle != null)
            {
                //NOTE: Moveの時点で姿勢が決定してるはずなので、単に実行する
                _penTabletEndParticle.Play();
            }
        }
        private void ClearKeyAndPadParticles()
        {
            for (int i = 0; i < _keyParticles.Length; i++)
            {
                Destroy(_keyParticles[i].gameObject);
            }
            _keyParticles = new ParticleSystem[0];

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

        private void ClearArcadeStickParticles()
        {
            for (int i = 0; i < _arcadeStickParticles.Length; i++)
            {
                Destroy(_arcadeStickParticles[i].gameObject);
            }
            _arcadeStickParticles = new ParticleSystem[0];
        }
        
        private void ClearMidiParticles()
        {
            for (int i = 0; i < _midiParticles.Length; i++)
            {
                Destroy(_midiParticles[i].gameObject);
            }
            _midiParticles = new ParticleSystem[0];
        }

        private void ClearPenTabletParticle()
        {
            if (_penTabletContinueParticle != null)
            {
                Destroy(_penTabletContinueParticle.gameObject);
                _penTabletContinueParticle = null;
            }

            if (_penTabletEndParticle != null)
            {
                Destroy(_penTabletEndParticle.gameObject);
                _penTabletEndParticle = null;
            }
        }
        
        private void SetupKeyboardParticle(int index)
        {
            var prefabSource = particlePrefabs[index];

            //パーティクルを有効化する場合
            _keyParticles = new ParticleSystem[particleStoreCount];
            for (int i = 0; i < _keyParticles.Length; i++)
            {
                _keyParticles[i] = Instantiate(prefabSource.prefab, this.transform).GetComponent<ParticleSystem>();
                if (prefabSource.useCollisionPlane)
                {
                    _keyParticles[i].collision.SetPlane(0, prefabSource.CollisionTransform);
                }

                //NOTE: ここではパーティクルの基準サイズのみを適用し、実行段階でキーボードのサイズに即したスケーリングを追加的に調整。
                //this.transform.localScale = prefabSource.scale;
            }
        }

        private void SetupArcadeStickParticle(int index)
        {
            var prefabSource = particlePrefabs[index];

            //パーティクルを有効化する場合
            _arcadeStickParticles = new ParticleSystem[particleStoreCount];
            for (int i = 0; i < _arcadeStickParticles.Length; i++)
            {
                _arcadeStickParticles[i] = Instantiate(prefabSource.prefab, this.transform).GetComponent<ParticleSystem>();
                //NOTE: アケコンからテキストがでてくるとテキストが落下する感じになって見栄えに違和感が出るが、
                //普通やらない組み合わせである + このためだけにcollision planeの追加設定したくないためスルー
            }
        }

        private void SetupMidiParticle(int index)
        {
            var prefabSource = midiParticlePrefabs[index];

            _midiParticles = new ParticleSystem[particleStoreCount];
            for (int i = 0; i < _midiParticles.Length; i++)
            {
                _midiParticles[i] = Instantiate(prefabSource.prefab, this.transform).GetComponent<ParticleSystem>();
                if (prefabSource.useCollisionPlane)
                {
                    _midiParticles[i].collision.SetPlane(0, prefabSource.CollisionTransform);
                }

                //NOTE: ここではパーティクルの基準サイズのみを適用し、実行段階でキーボードのサイズに即したスケーリングを追加的に調整。
                //this.transform.localScale = prefabSource.scale;
            }
        }

        private void SetupMouseParticle(int index)
        {
            _mouseContinueParticle = Instantiate(
                mouseParticlePrefabs[index].continueParticlePrefab, 
                _mouseParticlePrefabParent
            );
            _mouseContinueParticle.transform.localPosition = Vector3.zero;
            _mouseContinueParticle.transform.localRotation = Quaternion.identity;

            _mouseEndParticle = Instantiate(
                mouseParticlePrefabs[index].clickParticlePrefab,
                _mouseParticlePrefabParent
            );
            _mouseEndParticle.transform.localPosition = Vector3.zero;
            _mouseEndParticle.transform.localRotation = Quaternion.identity;
        }

        private void SetupPenTabletParticle(int index)
        {
            _penTabletContinueParticle = Instantiate(mouseParticlePrefabs[index].continueParticlePrefab);
            _penTabletContinueParticle.transform.localPosition = Vector3.zero;
            _penTabletContinueParticle.transform.localRotation = Quaternion.identity;

            _penTabletEndParticle = Instantiate(mouseParticlePrefabs[index].clickParticlePrefab);
            _penTabletEndParticle.transform.localPosition = Vector3.zero;
            _penTabletEndParticle.transform.localRotation = Quaternion.identity;
        }
    }
}
