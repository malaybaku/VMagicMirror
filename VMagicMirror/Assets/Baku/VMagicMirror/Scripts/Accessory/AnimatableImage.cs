using System;
using System.Linq;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public class AnimatableImage : IDisposable
    {
        //NOTE: Texture2D[] を渡すほうがきれいかもしれん
        public AnimatableImage(byte[][] binaries)
        {
            _allTextures = binaries
                .Select(bin =>
                {
                    var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
                    tex.LoadImage(bin);
                    tex.Apply();
                    return tex;
                })
                .ToArray();
        }

        private float _timePerFrame = 1f / 15f;
        private int _framePerSecond = 15;
        public int FramePerSecond
        {
            get => _framePerSecond;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException("FramePerSecond must be positive");
                }
                _framePerSecond = value;
                _timePerFrame = 1f / value;
            }
        }

        private Texture2D CurrentTexture
        {
            get
            {
                if (_imageIndex >= 0 && _imageIndex < _allTextures.Length)
                {
                    return _allTextures[_imageIndex];
                }
                else
                {
                    return null;
                }
            }
        }
        
        public Texture2D FirstTexture => _allTextures.Length > 0 ? _allTextures[0] : null;
        private Texture2D[] _allTextures;

        public Renderer Renderer { get; set; }

        private float _timeCount;
        private int _imageIndex;
        
        public void Update(float deltaTime)
        {
            _timeCount += deltaTime;
            if (_timeCount < _timePerFrame)
            {
                return;
            }

            _timeCount -= _timePerFrame;
            _imageIndex++;
            if (_imageIndex >= _allTextures.Length)
            {
                _imageIndex = 0;
            }

            if (Renderer != null)
            {
                Renderer.material.mainTexture = CurrentTexture;
            }
        }

        public void Dispose()
        {
            foreach (var texture in _allTextures)
            {
                UnityEngine.Object.Destroy(texture);
            }
            _allTextures = Array.Empty<Texture2D>();
        }
    }
}
