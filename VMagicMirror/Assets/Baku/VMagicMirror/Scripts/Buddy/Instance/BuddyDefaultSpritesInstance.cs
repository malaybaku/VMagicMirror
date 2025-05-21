using System;
using Baku.VMagicMirror.Buddy;
using UnityEngine;

namespace Baku.VMagicMirror
{
    // NOTE: 2D/3D双方で使えるように書けたらそうしたい…と思って若干ふわふわしたことを書いている
    public class BuddyDefaultSpritesInstance
    {
        public BuddyDefaultSpritesInstance(bool useSprite)
        {
            _useSprite = useSprite;
        }

        private readonly bool _useSprite;

        private bool _destroyOnClear;
        public bool HasValidSetup { get; private set; }

        public Texture2D CurrentTexture { get; private set; }
        public Sprite CurrentSprite { get; private set; }

        private Texture2D _defaultTexture;
        private Texture2D _blinkTexture;
        private Texture2D _mouthOpenTexture;
        private Texture2D _blinkMouthOpenTexture;

        private Sprite _defaultSprite;
        private Sprite _blinkSprite;
        private Sprite _mouthOpenSprite;
        private Sprite _blinkMouthOpenSprite;
        
        public void SetupTexture(
            bool destroyOnClear,
            Texture2D defaultTexture,
            Texture2D blinkTexture,
            Texture2D mouthOpenTexture,
            Texture2D blinkMouthOpenTexture)
        {
            if (_useSprite)
            {
                throw new InvalidOperationException("mode is incorrect");
            }

            _destroyOnClear = destroyOnClear;

            _defaultTexture = defaultTexture;
            _blinkTexture = blinkTexture;
            _mouthOpenTexture = mouthOpenTexture;
            _blinkMouthOpenTexture = blinkMouthOpenTexture;
            
            CurrentTexture = defaultTexture;
            HasValidSetup = true;
        }

        public void SetupSprite(
            bool destroyOnClear,
            Sprite defaultSprite,
            Sprite blinkSprite,
            Sprite mouthOpenSprite,
            Sprite blinkMouthOpenSprite)
        {
            if (!_useSprite)
            {
                throw new InvalidOperationException("mode is incorrect");
            }

            _destroyOnClear = destroyOnClear;

            _defaultSprite = defaultSprite;
            _blinkSprite = blinkSprite;
            _mouthOpenSprite = mouthOpenSprite;
            _blinkMouthOpenSprite = blinkMouthOpenSprite;

            CurrentSprite = defaultSprite;
            HasValidSetup = true;
        }
        
        public void SetState(BuddyDefaultSpriteState state)
        {
            if (_useSprite)
            {
                CurrentSprite = state switch
                {
                    BuddyDefaultSpriteState.Default => _defaultSprite,
                    BuddyDefaultSpriteState.Blink => _blinkSprite,
                    BuddyDefaultSpriteState.MouthOpen => _mouthOpenSprite,
                    BuddyDefaultSpriteState.BlinkMouthOpen => _blinkMouthOpenSprite,
                    _ => throw new ArgumentOutOfRangeException(nameof(state)),
                };
            }
            else
            {
                CurrentTexture = state switch
                {
                    BuddyDefaultSpriteState.Default => _defaultTexture,
                    BuddyDefaultSpriteState.Blink => _blinkTexture,
                    BuddyDefaultSpriteState.MouthOpen => _mouthOpenTexture,
                    BuddyDefaultSpriteState.BlinkMouthOpen => _blinkMouthOpenTexture,
                    _ => throw new ArgumentOutOfRangeException(nameof(state)),
                };
            }
        }

        // NOTE: 今のところClearするAPIは提供してないが、Disposeより早い段階でClearしてもよいのでpublicにしている
        public void Clear()
        {
            HasValidSetup = false;
            
            if (_useSprite)
            {
                if (_destroyOnClear)
                {
                    UnityEngine.Object.Destroy(_defaultSprite.texture);
                    UnityEngine.Object.Destroy(_blinkSprite.texture);
                    UnityEngine.Object.Destroy(_mouthOpenSprite.texture);
                    UnityEngine.Object.Destroy(_blinkMouthOpenSprite.texture);
                }

                // NOTE: Sprite自体は常にDestroyしてよい
                UnityEngine.Object.Destroy(_defaultSprite);
                UnityEngine.Object.Destroy(_blinkSprite);
                UnityEngine.Object.Destroy(_mouthOpenSprite);
                UnityEngine.Object.Destroy(_blinkMouthOpenSprite);
            }
            else
            {
                if (_destroyOnClear)
                {
                    UnityEngine.Object.Destroy(_defaultTexture);
                    UnityEngine.Object.Destroy(_blinkTexture);
                    UnityEngine.Object.Destroy(_mouthOpenTexture);
                    UnityEngine.Object.Destroy(_blinkMouthOpenTexture);
                }
            }

            CurrentSprite = null;
            _defaultSprite = null;
            _blinkSprite = null;
            _mouthOpenSprite = null;
            _blinkMouthOpenSprite = null;
            
            CurrentTexture = null;
            _defaultTexture = null;
            _blinkTexture = null;
            _mouthOpenTexture = null;
            _blinkMouthOpenTexture = null;

            _destroyOnClear = false;
        }

        public void Dispose() => Clear();
    }
}
