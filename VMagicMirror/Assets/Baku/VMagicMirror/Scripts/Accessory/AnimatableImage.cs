using System;
using System.Linq;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public class AnimatableImage : IDisposable, IAccessoryFileActions
    {
        //1コマ分の画像の表現方法
        enum TextureTypes
        {
            //何も表示しない = 指定されたテクスチャの全pixelが透明だったのでnullでも代用できるケース
            Empty,
            //Destroyが必要な画像
            Original,
            //全く同じ画像が他で指定されている = その画像のIndexが分かってれば画像を適用できる
            Referred,
        }
        
        class WrappedTexture
        {
            public WrappedTexture(TextureTypes type, Texture2D texture)
                :this (type, texture, Array.Empty<byte>())
            {
            }

            public WrappedTexture(TextureTypes type, Texture2D texture, byte[] bytes)
            {
                Type = type;
                Bytes = bytes;
                Texture = texture;
                RawSize = texture != null ? Math.Max(texture.width, texture.height) : 0;
            }
            
            public TextureTypes Type { get; }
            public byte[] Bytes { get; }
            public Texture2D Texture { get; set; }
            public int RawSize { get; }
        }

        //NOTE: アプリの生存期間中この1枚しか使わないため、特にDestroyもしない
        private static Texture2D _transparentTexture;
        private static Texture2D LoadTransparentTexture()
        {
            if (_transparentTexture == null)
            {
                _transparentTexture = new Texture2D(16, 16, TextureFormat.ARGB32, false);
                var colors = _transparentTexture.GetPixels32();
                for (var i = 0; i < colors.Length; i++)
                {
                    colors[i] = new Color32(0, 0, 0, 0);
                }
                _transparentTexture.SetPixels32(colors);
                _transparentTexture.Apply();
            }
            return _transparentTexture;
        }

        public AnimatableImage(byte[][] binaries)
        {
            var rawTextures = binaries
                .Select(bin =>
                {
                    var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
                    tex.LoadImage(bin);
                    tex.Apply();
                    return tex;
                })
                .ToArray();

            _textures = new WrappedTexture[rawTextures.Length];
            for (var i = 0; i < _textures.Length; i++)
            {
                //他に同じ画像があったら使い回す
                var hasSameBinary = false;
                for (var j = 0; j < i; j++)
                {
                    if (binaries[j].SequenceEqual(binaries[i]))
                    {
                        // 「透明テクスチャと一致する」というケースもあることに注意
                        _textures[i] = _textures[j].Type switch
                        {
                            TextureTypes.Empty => new WrappedTexture(TextureTypes.Empty, null),
                            TextureTypes.Original => new WrappedTexture(TextureTypes.Referred, _textures[j].Texture),
                            //来ないはず
                            _ => new WrappedTexture(TextureTypes.Empty, null),
                        };
                        Debug.Log($"discard duplicated animatable image frame, {i}, {_textures[i].Type}");
                        hasSameBinary = true;
                        break;
                    }
                }
                if (hasSameBinary)
                {
                    UnityEngine.Object.Destroy(rawTextures[i]);
                    continue;
                }
                
                //完全透明なテクスチャは捨てる: 透明であることの検出が重たいけど、これは一瞬なので我慢してもろて…
                var pixels = rawTextures[i].GetPixels32();
                if (pixels.All(p => p.a == 0))
                {
                    UnityEngine.Object.Destroy(rawTextures[i]);
                    _textures[i] = new WrappedTexture(TextureTypes.Empty, null);
                    Debug.Log($"discard totally transparent animatable image frame, {i}");
                    continue;
                }
                
                _textures[i] = new WrappedTexture(TextureTypes.Original, rawTextures[i], binaries[i]);
            }
        }

        private WrappedTexture[] _textures;

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

        //NOTE: 連番画像で透明な画像が入っている場合、正常な連番pngであってもnullが戻る事がある
        private Texture2D CurrentTexture
        {
            get
            {
                if (_imageIndex >= 0 && _imageIndex < _textures.Length)
                {
                    return _textures[_imageIndex].Texture;
                }
                else
                {
                    return null;
                }
            }
        }

        //NOTE: nullを戻す事があるのはby-design
        public Texture2D FirstValidTexture 
            => _textures.FirstOrDefault(t => t.Type != TextureTypes.Empty).Texture;

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
            if (_imageIndex >= _textures.Length)
            {
                _imageIndex = 0;
            }

            if (Renderer != null)
            {
                Renderer.material.mainTexture = CurrentTexture;
                if (CurrentTexture == null)
                {
                    Renderer.material.mainTexture = LoadTransparentTexture();
                }
            }
        }

        public void UpdateLayout(AccessoryItemLayout layout)
        {
            if (layout.FramePerSecond < 5 || layout.FramePerSecond > 30)
            {
                //0とかの不正値が来たら初期値(15)を適用する、ということ
                FramePerSecond = 15;
            }
            else
            {
                FramePerSecond = layout.FramePerSecond;
            }

            var maxSize = layout.GetResolutionLimitSize();
            foreach (var t in _textures.Where(t => t.Type == TextureTypes.Original))
            {
                AccessoryTextureResizer.ResizeImage(t.Texture, maxSize, t.Bytes, t.RawSize);
            }
        }

        public void OnVisibilityChanged(bool isVisible)
        {
            if (isVisible)
            {
                return;
            }

            _timeCount = 0f;
            _imageIndex = 0;
            if (Renderer != null)
            {
                Renderer.material.mainTexture = CurrentTexture;
            }
        }
        
        public void Dispose()
        {
            foreach (var t in _textures)
            {
                if (t.Type == TextureTypes.Original && 
                    t.Texture != null)
                {
                    UnityEngine.Object.Destroy(t.Texture);
                }
            }
            
            _textures = Array.Empty<WrappedTexture>();
        }
    }
}
