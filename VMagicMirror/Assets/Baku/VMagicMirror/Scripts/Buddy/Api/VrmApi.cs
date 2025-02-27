using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Baku.VMagicMirror.Buddy.Api.Interface;
using Cysharp.Threading.Tasks;

namespace Baku.VMagicMirror.Buddy.Api
{
    public class VrmApi : IVrmApi
    {
        public VrmApi(string baseDir, string buddyId, BuddyVrmInstance instance)
        {
            _baseDir = baseDir;
            _buddyId = buddyId;
            _instance = instance;
        }

        private readonly string _baseDir;
        private readonly string _buddyId;
        private readonly BuddyVrmInstance _instance;
        
        public Vector3 LocalPosition
        {
            get => _instance.LocalPosition.ToApiValue();
            set => _instance.LocalPosition = value.ToEngineValue();
        }

        public Quaternion LocalRotation
        {
            get => _instance.LocalRotation.ToApiValue();
            set => _instance.LocalRotation = value.ToEngineValue();
        }

        public Vector3 LocalScale
        {
            get => _instance.LocalScale.ToApiValue();
            set => _instance.LocalScale = value.ToEngineValue();
        }

        public Vector3 GetPosition() => _instance.GetWorldPosition().ToApiValue();
        public Quaternion GetRotation() => _instance.GetWorldRotation().ToApiValue();
        public void SetPosition(Vector3 position) => _instance.SetWorldPosition(position.ToEngineValue());
        public void SetRotation(Quaternion rotation) => _instance.SetWorldRotation(rotation.ToEngineValue());

        public void SetParent(ITransform3DApi parent)
        {
            var parentInstance = ((Transform3DApi)parent).GetInstance();
            _instance.SetParent(parentInstance);
        }

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
        {
            throw new NotImplementedException();
        }

        public void SetHipsPosition(Vector3 position)
        {
            throw new NotImplementedException();
        }

        public void SetBoneRotations(IReadOnlyDictionary<HumanBodyBones, Quaternion> localRotations)
        {
            throw new NotImplementedException();
        }

        public void SetMuscles(float?[] muscles)
        {
            throw new NotImplementedException();
        }
        
        public string[] GetCustomBlendShapeNames()
        {
            throw new System.NotImplementedException();
        }

        public bool HasBlendShape(string name)
        {
            throw new System.NotImplementedException();
        }

        public float GetBlendShape(string name)
        {
            throw new System.NotImplementedException();
        }

        public void SetBlendShape(string name, float value)
        {
            throw new NotImplementedException();
        }

        public async Task PreloadVrmAnimationAsync(string path)
        {
            await UniTask.SwitchToMainThread();
            await _instance.PreloadAnimationAsync(path);
        }
        
        public void RunVrma(string path, bool immediate)
        {
            var fullPath = Path.Combine(_baseDir, path);
            _instance.RunVrma(fullPath, immediate);
        }

        public void StopVrma()
        {
            _instance.StopVrma();
        }
    }
}
