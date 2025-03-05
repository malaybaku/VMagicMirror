using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using UniGLTF;
using UnityEngine;
using UniVRM10;
using VRMShaders;

namespace Baku.VMagicMirror.Buddy
{
    public class BuddyVrmInstance : MonoBehaviour
    {
        private bool _hasModel = false;
        // NOTE: このインスタンスは存在する場合BuddyVrmInstanceの子要素になっている(ので、オブジェクトごと破棄することができる)
        private Vrm10Instance _instance = null;
        private Animator _animator;

        private readonly Dictionary<string, Vrm10AnimationInstance> _animations = new();
        private Vrm10AnimationInstance _prevAnim;
        private Vrm10AnimationInstance _anim;

        public BuddyTransform3DInstance GetTransform3D()
        {
            var instance = GetComponent<BuddyTransform3DInstance>();
            if (instance != null)
            {
                return instance;
            }
            return gameObject.AddComponent<BuddyTransform3DInstance>();
        }

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
                _animator = instance.GetComponent<Animator>();
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
                _hasModel = true;
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
            _hasModel = false;
            if (_instance != null)
            {
                Destroy(_instance.gameObject);
            }

            _instance = null;
            _animator = null;
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

        public void StopVrma(bool immediate)
        {
            if (!_hasModel)
            {
                return;
            }

            _instance.Runtime.VrmAnimation = null;
        }

        public void SetBoneRotation(HumanBodyBones bone, Quaternion localRotation)
        {
            if (!_hasModel)
            {
                return;
            }
            throw new NotImplementedException();
        }

        public void SetHipsLocalPosition(Vector3 position)
        {
            if (!_hasModel)
            {
                return;
            }

            var hipsBone = _animator.GetBoneTransform(HumanBodyBones.Hips);
            // 親がnullかどうかを見ているが、より具体的にはTransform3Dが親要素として割り当たってるかどうかを見ている
            if (transform.parent != null)
            {
                var baseTransform = transform.parent;
                var worldPosition = baseTransform.TransformPoint(position);
                hipsBone.position = worldPosition;
            }
            else
            {
                hipsBone.position = position;
            }
        }

        public void SetHipsPosition(Vector3 position)
        {
            if (!_hasModel)
            {
                return;
            }
            
            var hipsBone = _animator.GetBoneTransform(HumanBodyBones.Hips);
            hipsBone.position = position;
        }

        public void SetMuscles(float?[] muscles)
        {
            if (!_hasModel || muscles == null)
            {
                return;
            }

            // TODO: Rootが間違ってるかも知れないので注意
            var handler = new HumanPoseHandler(_animator.avatar, _instance.transform);
            HumanPose pose = default;
            handler.GetHumanPose(ref pose);

            // NOTE: このdestMusclesはfield値なので再代入しないでOK
            var destMuscles = pose.muscles;
            for (var i = 0; i < destMuscles.Length; i++)
            {
                if (i >= muscles.Length)
                {
                    // 引数の配列が短すぎると通る。普通は通らない想定
                    break;
                }

                var v = muscles[i];
                if (v.HasValue)
                {
                    destMuscles[i] = v.Value;
                }
            }
            
            handler.SetHumanPose(ref pose);
        }

        public string[] GetCustomBlendShapeNames()
        {
            if (!_hasModel)
            {
                return Array.Empty<string>();
            }

            return _instance.Runtime
                .Expression
                .ExpressionKeys
                .Where(key => key.Preset == ExpressionPreset.custom)
                .Select(key => key.Name)
                .ToArray();
        }

        public bool HasCustomBlendShape(string s)
        {
            if (!_hasModel)
            {
                return false;
            }

            return _instance.Runtime
                .Expression
                .ExpressionKeys
                .Any(key => key.Preset == ExpressionPreset.custom && key.Name == s);
        }

        public float GetBlendShapeValue(string s, bool customClip)
        {
            if (!_hasModel)
            {
                return 0f;
            }

            var key = CreateExpressionKey(s, customClip);

            return _instance.Runtime
                .Expression
                .ActualWeights
                .GetValueOrDefault(key, 0f);
        }

        public void SetBlendShapeValue(string s, bool customClip, float value)
        {
            if (!_hasModel)
            {
                return;
            }
            
            var key = CreateExpressionKey(s, customClip);
            _instance.Runtime.Expression.SetWeight(key, value);
        }

        private static ExpressionKey CreateExpressionKey(string s, bool customClip)
        {
            return !customClip && Enum.TryParse<ExpressionPreset>(s, out var preset)
                ? ExpressionKey.CreateFromPreset(preset)
                : ExpressionKey.CreateCustom(s);
        }

        public float GetVrmaLength(string fullPath)
        {
            throw new NotImplementedException();
        }
    }
}
