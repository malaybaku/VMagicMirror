using System;
using System.IO;
using System.Linq;
using UniGLTF;
using UnityEngine;
using UnityEngine.Rendering;
using UniVRM10;

namespace Baku.VMagicMirror.Buddy
{
    public class BuddyGlbInstance : MonoBehaviour
    {
        private ImporterContext _importerContext = null;
        private RuntimeGltfInstance _gltfInstance = null;

        private bool _hasAnimationComponent = false;
        private Animation _gltfAnimation = null;
        
        // NOTE: setterはコンポーネントを生成するメソッドのみから用いる。InjectするほどでもないのでDIは使ってない
        public BuddyFolder BuddyFolder { get; set; }

        public BuddyTransform3DInstance GetTransform3D()
        {
            var instance = GetComponent<BuddyTransform3DInstance>();
            if (instance != null)
            {
                return instance;
            }
            return gameObject.AddComponent<BuddyTransform3DInstance>();
        }

        public void Load(string fullPath)
        {
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"GLB file does not exist at: " + fullPath);
            }

            var bytes = File.ReadAllBytes(fullPath);
            DeleteInstance();
            Load(bytes);
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
        
        // NOTE: 遷移をもっと丁寧にやれたほうがいいかもしれないが、割とラフにやってる
        public void RunAnimation(string animName, bool isLoop, bool immediate)
        {
            if (!_hasAnimationComponent)
            {
                return;
            }
            
            // NOTE: isLoopがoff -> onになりながらCrossFadeすると見た目がちょっと悪くなるかもなので注意
            _gltfAnimation.wrapMode = isLoop ? WrapMode.Loop : WrapMode.Once;
            if (immediate)
            {
                _gltfAnimation.Play(animName, PlayMode.StopAll);
            }
            else
            {
                _gltfAnimation.CrossFade(animName);
            }
        }

        public void StopAnimation()
        {
            if (!_hasAnimationComponent)
            {
                return;
            }

            _gltfAnimation.Stop();
        }

        private void DeleteInstance()
        {
            if (_gltfInstance != null)
            {
                _gltfInstance.Dispose();
            }
            _gltfInstance = null;

            _importerContext?.Dispose();
            _importerContext = null;

            _gltfAnimation = null;
            _hasAnimationComponent = false;
        }
        
        private void Load(byte[] bytes)
        {
            DeleteInstance();

            var parser = new GlbLowLevelParser("", bytes);
            using var data = parser.Parse();

            try
            {
                _importerContext = new ImporterContext(
                    data,
                    materialGenerator: new BuiltInVrm10MaterialDescriptorGenerator()
                    );
                _gltfInstance = _importerContext.Load();
                _gltfInstance.ShowMeshes();
                _gltfInstance.EnableUpdateWhenOffscreen();

                // NOTE: Shadowは常にオフか、というのは諸説ある
                foreach (var renderer in _gltfInstance.Root.GetComponentsInChildren<Renderer>())
                {
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }

                // NOTE: Animationをデフォルトで再生状態にするためにオフ→オンする (アクセサリーでやってるのと同じ処理)。
                // サブキャラの場合は明示的に実行するまでアニメーションしない択もあるのが悩ましいが…
                _gltfInstance.Root.SetActive(false);
                _gltfInstance.Root.SetActive(true);

                var t = _gltfInstance.transform;
                t.SetParent(transform);
                t.localPosition = Vector3.zero;
                t.localRotation = Quaternion.identity;
                t.localScale = Vector3.one;

                _gltfAnimation = _gltfInstance.Root.GetComponent<Animation>();
                _hasAnimationComponent = _gltfAnimation != null;
            }
            catch
            {
                DeleteInstance();
                throw;
            }
        }

        public string[] GetAnimationNames()
        {
            return _gltfInstance?.AnimationClips == null 
                ? Array.Empty<string>()
                : _gltfInstance.AnimationClips.Select(c => c.name).ToArray();
        }

        public void Dispose() => DeleteInstance();
    }
}
