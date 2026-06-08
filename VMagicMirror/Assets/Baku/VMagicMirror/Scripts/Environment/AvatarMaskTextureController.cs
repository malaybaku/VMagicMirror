using System;
using R3;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Zenject;

namespace Baku.VMagicMirror
{
    public sealed class AvatarMaskTextureController : PresenterBase, ILateTickable
    {
        public static AvatarMaskTextureController Instance { get; private set; }

        private readonly IVRMLoadable _vrmLoadable;
        private readonly Camera _targetCamera;

        // NOTE: Unityのnull checkをしたくないので明示的にフラグを持つ
        private bool _hasRenderTexture;
        private RenderTexture _avatarMaskTexture;
        private RenderTexture _avatarMaskDepthTexture;

        private readonly ReactiveProperty<bool> _useAvatarDropShadow = new(true);
        private readonly ReactiveProperty<bool> _useAvatarOffsetRim = new(false);
        private readonly ReactiveProperty<bool> _useAvatarMask = new(false);
        
        public float AvatarMaskOverscanFactor { get; } = 1.5f;

        public bool IsReady => _useAvatarMask.Value && _hasRenderTexture;
        
        public Renderer[] AvatarRenderers { get; private set; } = Array.Empty<Renderer>();
        public bool HasAvatar => AvatarRenderers.Length > 0;
        public RTHandle AvatarMaskHandle { get; private set; }
        public RTHandle AvatarMaskDepthHandle { get; private set; }

        public ReadOnlyReactiveProperty<bool> UseAvatarMask => _useAvatarMask;
        
        [Inject]
        public AvatarMaskTextureController(IVRMLoadable vrmLoadable, Camera mainCamera)
        {
            _vrmLoadable = vrmLoadable;
            _targetCamera = mainCamera;
        }

        public override void Initialize()
        {
            _vrmLoadable.VrmLoaded += info => AvatarRenderers = info.Renderers ?? Array.Empty<Renderer>();
            _vrmLoadable.VrmDisposing += () => AvatarRenderers = Array.Empty<Renderer>();

            _useAvatarDropShadow
                .CombineLatest(_useAvatarOffsetRim, (x, y) => x || y)
                .DistinctUntilChanged()
                .Subscribe(value =>
                {
                    if (value)
                    {
                        EnsureMaskTextures();
                    }
                    else
                    {
                        ReleaseMaskTextures();
                    }
                    _useAvatarMask.Value = value;
                })
                .AddTo(this);

            Instance = this;
        }

        public void LateTick()
        {
            if (_useAvatarMask.CurrentValue)
            {
                EnsureMaskTextures();
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            ReleaseMaskTextures();
        }
        
        public void SetAvatarDropShadowEnabled(bool enable) => _useAvatarDropShadow.Value = enable;
        public void SetAvatarOffsetRimEnabled(bool enable) => _useAvatarOffsetRim.Value = enable;
        
        private void EnsureMaskTextures()
        {
            var width = Mathf.Max(1, _targetCamera.scaledPixelWidth);
            var height = Mathf.Max(1, _targetCamera.scaledPixelHeight);
            if (_hasRenderTexture && 
                _avatarMaskTexture.width == width &&
                _avatarMaskTexture.height == height &&
                _avatarMaskDepthTexture.width == width &&
                _avatarMaskDepthTexture.height == height)
            {
                return;
            }

            ReleaseMaskTextures(); 

            var colorDescriptor = new RenderTextureDescriptor(width, height)
            {
                graphicsFormat = GraphicsFormat.R8_UNorm,
                depthStencilFormat = GraphicsFormat.None,
                msaaSamples = 1,
                mipCount = 1,
                volumeDepth = 1,
                dimension = TextureDimension.Tex2D,
                sRGB = false,
                useMipMap = false,
                autoGenerateMips = false
            };
            _avatarMaskTexture = new RenderTexture(colorDescriptor)
            {
                name = "VmmAvatarMask",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            _avatarMaskTexture.Create();
            AvatarMaskHandle = RTHandles.Alloc(_avatarMaskTexture);

            var depthDescriptor = new RenderTextureDescriptor(width, height)
            {
                graphicsFormat = GraphicsFormat.None,
                depthStencilFormat = GraphicsFormat.D24_UNorm_S8_UInt,
                msaaSamples = 1,
                mipCount = 1,
                volumeDepth = 1,
                dimension = TextureDimension.Tex2D,
                sRGB = false,
                useMipMap = false,
                autoGenerateMips = false
            };
            _avatarMaskDepthTexture = new RenderTexture(depthDescriptor)
            {
                name = "VmmAvatarMaskDepth",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            _avatarMaskDepthTexture.Create();
            AvatarMaskDepthHandle = RTHandles.Alloc(_avatarMaskDepthTexture);
            _hasRenderTexture = true;
        }

        private void ReleaseMaskTextures()
        {
            if (!_hasRenderTexture)
            {
                return;
            }

            _hasRenderTexture = false;
            AvatarMaskHandle?.Release();
            AvatarMaskDepthHandle?.Release();
            AvatarMaskHandle = null;
            AvatarMaskDepthHandle = null;

            if (_avatarMaskTexture.IsCreated())
            {
                _avatarMaskTexture.Release();
            }
            UnityEngine.Object.Destroy(_avatarMaskTexture);
            _avatarMaskTexture = null;

            if (_avatarMaskDepthTexture.IsCreated())
            {
                _avatarMaskDepthTexture.Release();
            }
            UnityEngine.Object.Destroy(_avatarMaskDepthTexture);
            _avatarMaskDepthTexture = null;
        }
    }
}
