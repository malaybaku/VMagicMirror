using UnityEngine;
using VMagicMirror.Buddy;
using BuddyApi = VMagicMirror.Buddy;

namespace Baku.VMagicMirror.Buddy.Api
{
    // NOTE: 内部のコードでAPI用の型を見に行かないで済むように二重定義している
    /// <summary>
    /// <see cref="BuddyApi.Sprite2DTransitionStyle"/> と同じやつ
    /// </summary>
    public enum Sprite2DTransitionStyle
    {
        None = 0,
        Immediate = 1,
        LeftFlip = 2,
        RightFlip = 3,
        BottomFlip = 4,
    }

    public class Sprite2DApi : ISprite2D
    {
        private readonly BuddySprite2DInstance _instance;
        private readonly BuddyLogger _logger;
        private BuddyFolder BuddyFolder { get; }

        internal Sprite2DApi(BuddyFolder buddyFolder, BuddySprite2DInstance instance, BuddyLogger logger)
        {
            BuddyFolder = buddyFolder;
            _instance = instance;
            _logger = logger;

            Transform = new Transform2D(instance.GetTransform2DInstance());
            DefaultSpritesSetting = new DefaultSpritesSettingApi(_instance.DefaultSpritesSetting);
        }
        
        public override string ToString() => nameof(ISprite2D);

        public ITransform2D Transform { get; }
        public IDefaultSpritesSetting DefaultSpritesSetting { get; }
        
        BuddyApi.Vector2 ISprite2D.Size
        {
            get => _instance.Size.ToApiValue();
            set => _instance.Size = value.ToEngineValue();
        }

        ISpriteEffect ISprite2D.Effects => _instance.SpriteEffects;

        public void Preload(string path) => ApiUtils.Try(BuddyFolder, _logger, () =>
        {
            var fullPath = GetFullPath(path);
            var result = _instance.Load(fullPath);
            HandleTextureLoadResult(fullPath, result);
        });

        public void Show(string path) => Show(path, BuddyApi.Sprite2DTransitionStyle.Immediate, 0f);

        public void Show(string path, BuddyApi.Sprite2DTransitionStyle style, float duration)
        {
            ApiUtils.Try(BuddyFolder, _logger, () =>
            {
                var fullPath = GetFullPath(path);
                var loadResult = _instance.Show(
                    fullPath,
                    GetClampedStyle(style),
                    Mathf.Max(duration, 0f)
                    );
                HandleTextureLoadResult(fullPath, loadResult);
                if (loadResult == TextureLoadResult.Success)
                {
                    _instance.SetActive(true);
                }
            });
        }

        public void ShowPreset(string name) => ShowPreset(name, BuddyApi.Sprite2DTransitionStyle.Immediate, 0f);

        public void ShowPreset(string name, BuddyApi.Sprite2DTransitionStyle style, float duration)
        {
            ApiUtils.Try(BuddyFolder, _logger, () =>
            {
                var clamped = UnityEngine.Mathf.Clamp(
                    (int)style, (int)Sprite2DTransitionStyle.None, (int)Sprite2DTransitionStyle.BottomFlip
                );

                // NOTE: Presetのロードエラーはコーディングの段階で間違ってないと起こらないので、エラーは起こるだけ繰り返し表示する
                var loadResult = _instance.ShowPreset(
                    name,
                    (Sprite2DTransitionStyle)clamped,
                    Mathf.Max(duration, 0f)
                    );
                HandlePresetTextureLoadResult(name, loadResult);
                if (loadResult == TextureLoadResult.Success)
                {
                    _instance.SetActive(true);
                }
            });
        }

        public void Hide() => _instance.SetActive(false);

        public void SetupDefaultSprites(
            string defaultImagePath,
            string blinkImagePath,
            string mouthOpenImagePath,
            string blinkMouthOpenImagePath
            ) => ApiUtils.Try(BuddyFolder, _logger, () =>
        {
            var setupResult = _instance.SetupDefaultSprites(
                GetFullPath(defaultImagePath),
                GetFullPath(blinkImagePath),
                GetFullPath(mouthOpenImagePath),
                GetFullPath(blinkMouthOpenImagePath)
            );

            HandleTextureLoadResult(defaultImagePath, setupResult.Item1);
            HandleTextureLoadResult(blinkImagePath, setupResult.Item2);
            HandleTextureLoadResult(mouthOpenImagePath, setupResult.Item3);
            HandleTextureLoadResult(blinkMouthOpenImagePath, setupResult.Item4);
        });

        // NOTE: このメソッドは内部的なコーディングエラーが無い限り成功するはず
        public void SetupDefaultSpritesByPreset() => ApiUtils.Try(BuddyFolder, _logger, () =>
        {
            _instance.SetupDefaultSpritesByPreset();
        });
        
        public void ShowDefaultSprites() => ShowDefaultSprites(BuddyApi.Sprite2DTransitionStyle.Immediate, 0f);

        public void ShowDefaultSprites(BuddyApi.Sprite2DTransitionStyle style, float duration) 
            => ApiUtils.Try(BuddyFolder, _logger, () =>
            {
                var willSuccess = _instance.ShowDefaultSprites(
                    GetClampedStyle(style),
                    Mathf.Max(duration, 0f)
                );
                if (willSuccess)
                {
                    _instance.SetActive(true);
                }
            });

        private string GetFullPath(string path) => ApiUtils.GetAssetFullPath(BuddyFolder, path);

        private void HandleTextureLoadResult(string fullPath, TextureLoadResult loadResult)
        {
            if (loadResult is TextureLoadResult.FailureFileNotFound)
            {
                _logger.Log(BuddyFolder, "Specified file does not exist: " + fullPath, BuddyLogLevel.Fatal);
            }
        }

        private void HandlePresetTextureLoadResult(string name, TextureLoadResult result)
        {
            if (result is TextureLoadResult.FailureFileNotFound)
            {
                _logger.Log(BuddyFolder, "Specified preset does not exist: " + name, BuddyLogLevel.Fatal);
            }
        }

        private static Sprite2DTransitionStyle GetClampedStyle(BuddyApi.Sprite2DTransitionStyle style)
        {
            var clamped = Mathf.Clamp(
                (int)style, (int)Sprite2DTransitionStyle.None, (int)Sprite2DTransitionStyle.BottomFlip
            );
            return (Sprite2DTransitionStyle)clamped;
        }
    }
}