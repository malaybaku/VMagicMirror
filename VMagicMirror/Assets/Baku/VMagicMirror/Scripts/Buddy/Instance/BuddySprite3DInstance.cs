using System;
using UnityEngine;

namespace Baku.VMagicMirror.Buddy
{
    // TODO: 2Dと同じく、PointerEnter的なイベントに反応できてほしいので、やり方を考えてね
    public class BuddySprite3DInstance : BuddyObject3DInstanceBase
    {
        private const float TransitionDuration = 0.5f;

        [SerializeField] private SpriteRenderer spriteRenderer;

        // NOTE: 呼び出し元がSprite/Texture2Dの破棄に責任を持つ前提でこうしてる
        public Sprite CreateSprite(Texture2D texture)
        {
            // NOTE: 長いほうが常に1mになるようにする。漫符等の装飾ではこのスケールが邪魔になることもあるが、そこは調整してもらう感じで…
            var pixelsPerUnit = Mathf.Max(texture.width, texture.height);
            return Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.0f), pixelsPerUnit, 0,
                SpriteMeshType.FullRect);
        }
        
        public void SetSprite(Sprite sprite) => spriteRenderer.sprite = sprite;

        // TODOかも: Apiのレベルでファイルからのロード + Spriteのキャッシュまではしても良いかも
        // (キャッシュはInstance側のような気もするが)
        public void Preload(string path) => throw new NotImplementedException();
        public void Show(string path) => throw new NotImplementedException();
        public void Hide() => gameObject.SetActive(false);
    }
}
