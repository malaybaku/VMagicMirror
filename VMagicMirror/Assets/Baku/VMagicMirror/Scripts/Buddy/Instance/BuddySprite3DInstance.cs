using System;
using System.Collections.Generic;
using Baku.VMagicMirror.Buddy.Api;
using UnityEngine;

namespace Baku.VMagicMirror.Buddy
{
    public enum Sprite3DTransitionStyle
    {
        None = 0,
        Immediate = 1,
        LeftFlip = 2,
        RightFlip = 3,
    }
    
    // TODO: 2Dと同じく、PointerEnter的なイベントに反応できてほしいので、やり方を考えてね
    public class BuddySprite3DInstance : BuddyObject3DInstanceBase
    {
        private const float TransitionDuration = 0.5f;

        [SerializeField] private SpriteRenderer spriteRenderer;

        // NOTE: CurrentTransitionStyleがNone以外な場合、このテクスチャが実際に表示されているとは限らない
        public Sprite CurrentSprite { get; private set; }

        internal Sprite3DTransitionStyle CurrentTransitionStyle { get; set; } = Sprite3DTransitionStyle.None;

        private readonly Dictionary<string, Sprite> _sprites = new();

        public void SetActive(bool active) => gameObject.SetActive(active);

        public void Preload(string fullPath) => Load(fullPath);

        public void Show(string fullPath) => Show(fullPath, Sprite3DTransitionStyle.Immediate);

        /// <summary>
        /// 必要なら画像のロードを行ったうえで表示する
        /// </summary>
        /// <param name="fullPath"></param>
        /// <param name="style"></param>
        /// <returns></returns>
        public TextureLoadResult Show(string fullPath, Sprite3DTransitionStyle style)
        {
            if (!_sprites.ContainsKey(fullPath))
            {
                var loadResult = Load(fullPath);
                if (loadResult is not TextureLoadResult.Success)
                {
                    return loadResult;
                }
            }

            CurrentSprite = _sprites[fullPath];
            CurrentTransitionStyle = style;
            
            return TextureLoadResult.Success;
        }
        
        /// <summary>
        /// 画像のロード処理はするが、ただちに表示するわけではなく、キャッシュにデータを乗せる
        /// </summary>
        /// <param name="fullPath"></param>
        /// <returns></returns>
        private TextureLoadResult Load(string fullPath)
        {
            if (_sprites.ContainsKey(fullPath))
            {
                return TextureLoadResult.Success;
            }
            
            var loadResult = ApiUtils.TryGetTexture2D(fullPath, out var texture);
            if (loadResult is TextureLoadResult.Success)
            {
                _sprites[fullPath] = CreateSprite(texture);
            }
            
            return loadResult;
        }        

        private Sprite CreateSprite(Texture2D texture)
        {
            // NOTE: 長いほうが常に1mになるようにする。漫符等の装飾ではこのスケールが邪魔になることもあるが、そこは調整してもらう感じで…
            var pixelsPerUnit = Mathf.Max(texture.width, texture.height);
            return Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.0f), pixelsPerUnit, 0,
                SpriteMeshType.FullRect);
        }

        public void DoTransition(float deltaTime)
        {
            // 一旦トランジション処理そのものは考慮せず、単にスプライトを直ちに更新する
            spriteRenderer.sprite = CurrentSprite;
            CurrentTransitionStyle = Sprite3DTransitionStyle.None;
        }
        
        public void Dispose()
        {
            CurrentSprite = null;
            foreach (var t in _sprites.Values)
            {
                Destroy(t.texture);
                Destroy(t);
            }
            _sprites.Clear();

            Destroy(gameObject);
        }
    }
}
