using System;
using System.Collections.Generic;
using System.IO;
using Baku.VMagicMirror.Buddy.Api.Interface;
using UnityEngine;
using Object = UnityEngine.Object;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Baku.VMagicMirror.Buddy.Api
{
    public enum Sprite2DTransitionStyle
    {
        None = 0,
        Immediate = 1,
        LeftFlip = 2,
        RightFlip = 3,
    }

    public class Sprite2DApi : ISprite2DApi
    {
        // 毎フレームログ出力しないように…ということで
        private bool _fileNotFoundErrorLogged = false;
        private bool _pathInvalidErrorLogged = false;

        internal BuddySpriteInstance Instance { get; set; }

        // NOTE: CurrentTransitionStyleがNone以外な場合、このテクスチャが実際に表示されているとは限らない
        internal Texture2D CurrentTexture { get; private set; }

        // NOTE: この値はトランジション処理をやっているクラスがトランジションを完了すると、自動でNoneに切り替わる
        internal Sprite2DTransitionStyle CurrentTransitionStyle { get; set; } = Sprite2DTransitionStyle.None;

        internal bool IsActive { get; private set; } = true;

        private readonly Dictionary<string, Texture2D> _textures = new();

        internal Vector2 InternalPosition { get; set; }

        Interface.Vector2 ISprite2DApi.Position
        {
            get => InternalPosition.ToApiValue();
            set => InternalPosition = value.ToEngineValue();
        }

        internal Vector2 InternalSize { get; set; }

        Interface.Vector2 ISprite2DApi.Size
        {
            get => InternalSize.ToApiValue();
            set => InternalSize = value.ToEngineValue();
        }

        // NOTE: scale/pivotはエフェクトからは制御しないのでこうなってる
        // Effectからも制御する場合、普通に値だけ保持する方向に修正する
        // (scaleは特に「ぷにぷに」とか実装したら修正しそう…)
        internal Vector2 InternalScale
        {
            get => Instance.RectTransform.localScale;
            set => Instance.RectTransform.localScale = new Vector3(value.x, value.y, 1f);
        }

        Interface.Vector2 ISprite2DApi.Scale
        {
            get => InternalScale.ToApiValue();
            set => InternalScale = value.ToEngineValue();
        }


        internal Vector2 InternalPivot
        {
            get => Instance.RectTransform.pivot;
            set => Instance.RectTransform.pivot = value;
        }
        
        Interface.Vector2 ISprite2DApi.Pivot
        {
            get => InternalPivot.ToApiValue();
            set => InternalPivot = value.ToEngineValue();
        }

        public SpriteEffectApi InternalEffects { get; } = new();
        // NOTE: ここだけEffectApi自体がデータ的に定義されてるので素朴にnew()してよい
        public ISpriteEffectApi Effects => InternalEffects;

        private readonly string _baseDir;
        private readonly string _buddyId;

        internal Sprite2DApi(string baseDir, string buddyId)
        {
            _baseDir = baseDir;
            _buddyId = buddyId;
        }

        internal void Dispose()
        {
            CurrentTexture = null;
            foreach (var t in _textures.Values)
            {
                Object.Destroy(t);
            }

            _textures.Clear();
            if (Instance != null)
            {
                Object.Destroy(Instance.gameObject);
            }

            Instance = null;
        }

        public void Hide() => IsActive = false;

        public void Show(string path) => Show(path, Interface.Sprite2DTransitionStyle.Immediate);

        public void Show(string path, Interface.Sprite2DTransitionStyle style)
        {
            ApiUtils.Try(_buddyId, () =>
            {
                var fullPath = Path.Combine(_baseDir, path).ToLower();
                if (!Load(fullPath))
                {
                    return;
                }

                CurrentTexture = _textures[fullPath];
                var clampedStyle = Mathf.Clamp(
                    (int)style, (int)Sprite2DTransitionStyle.None, (int)Sprite2DTransitionStyle.RightFlip
                );
                CurrentTransitionStyle = (Sprite2DTransitionStyle)clampedStyle;
                IsActive = true;
            });
        }

        public void Preload(string path)
        {
            ApiUtils.Try(_buddyId, () =>
            {
                var fullPath = Path.Combine(_baseDir, path).ToLower();
                Load(fullPath);
            });
        }

        public void SetPosition(Interface.Vector2 position)
        {
            throw new NotImplementedException();
        }

        public void SetParent(ITransform2DApi parent)
        {
            var parentInstance = ((Transform2DApi)parent).GetInstance();
            Instance.SetParent(parentInstance);
        }

        private bool Load(string fullPath)
        {
            if (_textures.ContainsKey(fullPath))
            {
                // cacheを使えばよい
                return true;
            }

            if (!ApiUtils.IsInBuddyDirectory(fullPath))
            {
                if (!_pathInvalidErrorLogged)
                {
                    LogOutput.Instance.Write("Specified path is not in Buddy directory: " + fullPath);
                }

                _pathInvalidErrorLogged = true;
                return false;
            }

            if (!File.Exists(fullPath))
            {
                if (!_fileNotFoundErrorLogged)
                {
                    LogOutput.Instance.Write("File not found: " + fullPath);
                }

                _fileNotFoundErrorLogged = true;
                return false;
            }


            var bytes = File.ReadAllBytes(fullPath);
            var texture = new Texture2D(32, 32);
            texture.LoadImage(bytes);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.Apply(false, true);
            _textures[fullPath] = texture;
            return true;
        }
    }
}