using System;
using System.Collections.Generic;
using Baku.VMagicMirror.Buddy.Api.Interface;

namespace Baku.VMagicMirror.Buddy.Api
{
    public class VrmApi : IVrmApi
    {
        public VrmApi(BuddyVrmInstance instance)
        {
            _instance = instance;
        }

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

        public Vector2 LocalScale
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

        public void Hide() => _instance.Hide();

        public void Load(string path)
        {
            throw new NotImplementedException();
        }

        public void Show()
        {
            throw new NotImplementedException();
        }

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

        public void RunVrma(string path, bool immediate)
        {
            throw new NotImplementedException();
        }

        public void StopVrma()
        {
            throw new NotImplementedException();
        }
    }
}
