using System;
using UnityEngine;
using UniVRM10;
using Object = UnityEngine.Object;

namespace Baku.VMagicMirror
{
    public class VrmaInstance : IEquatable<VrmaInstance>
    {
        private bool _disposed;

        public VrmaFileItem File { get; }
        public Vrm10AnimationInstance Instance { get; }
        public Animation Animation { get; }

        public VrmaInstance(VrmaFileItem file, Vrm10AnimationInstance instance, Animation animation)
        {
            File = file;
            Instance = instance;
            Animation = animation;
            _disposed = false;
        }

        public void PlayFromStart(bool isLoop)
        {
            if (_disposed)
            {
                return;
            }
                
            Animation.Rewind();
            Animation.wrapMode = isLoop ? WrapMode.Loop : WrapMode.Once;
            Animation.Play();
        }

        public void Stop()
        {
            if (_disposed)
            {
                return;
            }

            Animation.Stop();
        }
            
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Object.Destroy(Instance.gameObject);
            _disposed = true;
        }
            
        public bool Equals(VrmaInstance other) => other is { } && File.Equals(other.File);
        public override bool Equals(object obj) => obj is VrmaInstance other && Equals(other);
        public override int GetHashCode() => File.GetHashCode();
    }
}
