using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UniGLTF;
using UnityEngine;
using UniVRM10;
using VRMShaders;

namespace Baku.VMagicMirror.Buddy
{
    public class BuddyVrmInstance : BuddyObject3DInstanceBase
    {
        private bool _hasModel = false;
        // NOTE: このインスタンスは存在する場合BuddyVrmInstanceの子要素になっている(ので、オブジェクトごと破棄することができる)
        private Vrm10Instance _instance = null;

        private readonly Dictionary<string, Vrm10AnimationInstance> _animations = new();
        private Vrm10AnimationInstance _prevAnim;
        private Vrm10AnimationInstance _anim;

        public void UpdateInstance()
        {
            if (_hasModel)
            {
                _instance.Runtime.Process();
            }
        }
        
        public async UniTask LoadAsync(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogError("VRM not found");
                return;
            }

            if (Path.GetExtension(path).ToLower() != ".vrm")
            {
                // NOTE: メインのVRM10LoadControllerと違って「拡張子がわざと変えてある」程度なら通しても良い可能性もある
                Debug.LogError("VRM extension is invalid");
                return;
            }

            ReleaseCurrentVrm();
            Vrm10Instance instance = null;
            try
            {
                var bytes = await File.ReadAllBytesAsync(path);
                Debug.LogWarning($"Start Loading VRM at :{path}");
                instance = await Vrm10.LoadBytesAsync(bytes,
                    true,
                    ControlRigGenerationOption.Generate,
                    true,
                    ct: this.GetCancellationTokenOnDestroy()
                );

                //NOTE: VRM10LoadControllerでもやってる処理なのでこっちでもやっている
                foreach(var sr in instance.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    sr.updateWhenOffscreen = true;
                }

                _instance = instance;
                //NOTE: Script Execution OrderをVMM側で制御したいのでこうする
                instance.UpdateType = Vrm10Instance.UpdateTypes.None;
                
                var renderers = instance.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                {
                    //セルフシャドウは明示的に切る: ちょっとでも軽量化したい
                    r.receiveShadows = false;
                }

                var t = _instance.transform;
                t.SetParent(transform);
                t.localPosition = Vector3.zero;
                t.localRotation = Quaternion.identity;
                t.localScale = Vector3.one;
                Debug.LogWarning($"Loading VRM done! at :{path}");
            }
            catch (Exception)
            {
                if (instance != null)
                {
                    Destroy(instance.gameObject);
                }
                throw;
            }
        }

        private void ReleaseCurrentVrm()
        {
            Debug.LogWarning($"Release current VRM...");
            if (_instance != null)
            {
                Destroy(_instance.gameObject);
            }

            _instance = null;
        }

        public void SetActive(bool active) => gameObject.SetActive(active);

        public void Dispose()
        {
            foreach (var anim in _animations.Values)
            {
                anim.GetComponent<RuntimeGltfInstance>().Dispose();
            }
            _animations.Clear();
            _anim = null;
            _prevAnim = null;

            Destroy(gameObject);
        }

        public async UniTask PreloadAnimationAsync(string fullPath)
        {
            using var data = new AutoGltfFileParser(fullPath).Parse();
            using var loader = new VrmAnimationImporter(data);
            var gltfInstance = await loader.LoadAsync(new ImmediateCaller());
            var instance = gltfInstance.GetComponent<Vrm10AnimationInstance>();
            instance.ShowBoxMan(false);

            _animations[fullPath] = instance;
        }
        
        public void RunVrma(string fullPath, bool immediate)
        {
            if (!_animations.ContainsKey(fullPath) || !_hasModel)
            {
                return;
            }

            if (_prevAnim != null)
            {
                _prevAnim.Dispose();
            }
            _prevAnim = _anim;
            
            var anim = _animations[fullPath];
            _anim = anim;

            // TODO: コレだと補間ができないので、メインアバターでやってるのと同水準のことをしたい…
            _instance.Runtime.VrmAnimation = _anim;
        }

        public void StopVrma()
        {
            if (!_hasModel)
            {
                return;
            }

            _instance.Runtime.VrmAnimation = null;
        }
    }
}
