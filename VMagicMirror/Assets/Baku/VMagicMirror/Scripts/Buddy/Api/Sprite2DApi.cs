using System;
using System.IO;
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
        private readonly string _baseDir;
        private readonly BuddySprite2DInstance _instance;
        private readonly BuddyLogger _logger;
        private BuddyFolder BuddyFolder => _instance.BuddyFolder;

        private bool _fileNotFoundErrorLogged;
        private bool _pathInvalidErrorLogged;

        internal Sprite2DApi(string baseDir, BuddySprite2DInstance instance, BuddyLogger logger)
        {
            _baseDir = baseDir;
            _instance = instance;
            _logger = logger;

            Transform = new Transform2D(instance.GetTransform2DInstance());
            DefaultSpritesSetting = new DefaultSpritesSettingApi(_instance.DefaultSpritesSetting);
        }

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
            HandleTextureLoadResult(defaultImagePath, setupResult);
        });

        // NOTE: このメソッドは内部的なコーディングエラーが無い限り成功するはず
        public void SetupDefaultSpritesByPreset() => _instance.SetupDefaultSpritesByPreset();
        
        public void ShowDefaultSprites() => ShowDefaultSprites(BuddyApi.Sprite2DTransitionStyle.Immediate, 0f);
        public void ShowDefaultSprites(BuddyApi.Sprite2DTransitionStyle style, float duration)
        {
            _instance.ShowDefaultSprites(
                GetClampedStyle(style),
                Mathf.Max(duration, 0f) 
            );
        }

        private string GetFullPath(string path) => Path.Combine(_baseDir, path);

        private void HandleTextureLoadResult(string fullPath, TextureLoadResult loadResult)
        {
            if (loadResult is TextureLoadResult.FailurePathIsNotInBuddyDirectory)
            {
                if (!_pathInvalidErrorLogged)
                {
                    _logger.Log(BuddyFolder, "Specified path is not in Buddy directory: " + fullPath, BuddyLogLevel.Error);
                }

                _pathInvalidErrorLogged = true;
            }
            else if (loadResult is TextureLoadResult.FailureFileNotFound)
            {
                if (!_fileNotFoundErrorLogged)
                {
                    _logger.Log(BuddyFolder, "Specified file does not exist: " + fullPath, BuddyLogLevel.Error);
                }

                _fileNotFoundErrorLogged = true;
            }
        }

        private void HandlePresetTextureLoadResult(string name, TextureLoadResult result)
        {
            if (result is TextureLoadResult.FailureFileNotFound)
            {
                _logger.Log(BuddyFolder, "Specified preset does not exist: " + name, BuddyLogLevel.Error);
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