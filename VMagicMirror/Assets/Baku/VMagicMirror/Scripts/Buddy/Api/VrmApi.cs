using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using VMagicMirror.Buddy;
using Cysharp.Threading.Tasks;

namespace Baku.VMagicMirror.Buddy.Api
{
    public class VrmApi : IVrm
    {
        private readonly string _baseDir;
        private readonly BuddyVrmInstance _instance;
        private string BuddyId => _instance.BuddyId;

        public VrmApi(string baseDir, BuddyVrmInstance instance)
        {
            _baseDir = baseDir;
            _instance = instance;
            Transform = new Transform3D(instance.GetTransform3D());
        }

        public ITransform3D Transform { get; }

        public async Task LoadAsync(string path)
        {
            //TODO: パス不正とかファイルが見つからないとかの場合に1回だけエラーが出したい
            // Sprite2Dの処理を使い回せるとgood
            var fullPath = Path.Combine(_baseDir, path);
            await _instance.LoadAsync(fullPath);
        }

        public void Show() => _instance.SetActive(true);
        public void Hide() => _instance.SetActive(false);

        public void SetBoneRotation(HumanBodyBones bone, Quaternion localRotation)
            => _instance.SetBoneRotation(bone.ToEngineValue(), localRotation.ToEngineValue());
        public void SetHipsLocalPosition(Vector3 position) 
            => _instance.SetHipsLocalPosition(position.ToEngineValue());
        public void SetHipsPosition(Vector3 position) 
            => _instance.SetHipsPosition(position.ToEngineValue());

        public void SetBoneRotations(IReadOnlyDictionary<HumanBodyBones, Quaternion> localRotations)
        {
            // NOTE: Dictごと渡す方が早そうな場合、Keyの型変換をAPIで行うのをサボってもOK
            foreach (var (bone, rotation) in localRotations)
            {
                _instance.SetBoneRotation(bone.ToEngineValue(), rotation.ToEngineValue());
            }
        }

        public void SetMuscles(float?[] muscles) => _instance.SetMuscles(muscles);

        public string[] GetCustomBlendShapeNames() => _instance.GetCustomBlendShapeNames();

        public bool HasBlendShape(string name) => _instance.HasCustomBlendShape(name);

        public float GetBlendShape(string name, bool customClip) => _instance.GetBlendShapeValue(name, customClip);

        public void SetBlendShape(string name, bool customClip, float value)
            => _instance.SetBlendShapeValue(name, customClip, value);

        // TODO: たぶんVrma自体を別の型として分けるので、Preloadの概念はなくなりそう
        public async Task PreloadVrmAnimationAsync(string path)
        {
            await UniTask.SwitchToMainThread();
            await _instance.PreloadAnimationAsync(path);
        }
        
        public void RunVrma(string path, bool loop, bool immediate)
        {
            var fullPath = Path.Combine(_baseDir, path);
            _instance.RunVrma(fullPath, immediate);
        }

        public void StopVrma(bool immediate) => _instance.StopVrma(immediate);

        public float GetVrmaLength(string path)
        {
            var fullPath = Path.Combine(_baseDir, path);
            return _instance.GetVrmaLength(fullPath);
        }
    }
}
