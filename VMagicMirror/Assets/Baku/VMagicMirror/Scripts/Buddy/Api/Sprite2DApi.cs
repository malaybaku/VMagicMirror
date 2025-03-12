using System.IO;
using VMagicMirror.Buddy;
using BuddyApi = VMagicMirror.Buddy;

namespace Baku.VMagicMirror.Buddy.Api
{
    public enum Sprite2DTransitionStyle
    {
        None = 0,
        Immediate = 1,
        LeftFlip = 2,
        RightFlip = 3,
    }

    public class Sprite2DApi : ISprite2D
    {
        private readonly string _baseDir;
        private readonly BuddySprite2DInstance _instance;
        private readonly BuddyLogger _logger;
        private string BuddyId => _instance.BuddyId;

        private bool _fileNotFoundErrorLogged;
        private bool _pathInvalidErrorLogged;

        internal Sprite2DApi(string baseDir, BuddySprite2DInstance instance, BuddyLogger logger)
        {
            _baseDir = baseDir;
            _instance = instance;
            _logger = logger;
            Transform = new Transform2D(instance.GetTransform2DInstance());
        }

        public ITransform2D Transform { get; }

        Vector2 ISprite2D.Size
        {
            get => _instance.Size.ToApiValue();
            set => _instance.Size = value.ToEngineValue();
        }

        ISpriteEffect ISprite2D.Effects => _instance.SpriteEffects;

        public void Preload(string path) => ApiUtils.Try(BuddyId, _logger, () =>
        {
            var fullPath = GetFullPath(path);
            var result = _instance.Load(fullPath);
            HandleTextureLoadResult(fullPath, result);
        });

        public void Show(string path) => Show(path, BuddyApi.Sprite2DTransitionStyle.Immediate);

        public void Show(string path, BuddyApi.Sprite2DTransitionStyle style)
        {
            ApiUtils.Try(BuddyId, _logger, () =>
            {
                var fullPath = GetFullPath(path);
                var clamped = UnityEngine.Mathf.Clamp(
                    (int)style, (int)Sprite2DTransitionStyle.None, (int)Sprite2DTransitionStyle.RightFlip
                );

                var loadResult = _instance.Show(fullPath, (Sprite2DTransitionStyle)clamped);
                HandleTextureLoadResult(fullPath, loadResult);
                if (loadResult == TextureLoadResult.Success)
                {
                    _instance.SetActive(true);
                }
            });
        }

        public void Hide() => _instance.SetActive(false);

        private string GetFullPath(string path) => Path.Combine(_baseDir, path);

        private void HandleTextureLoadResult(string fullPath, TextureLoadResult loadResult)
        {
            if (loadResult is TextureLoadResult.FailurePathIsNotInBuddyDirectory)
            {
                if (!_pathInvalidErrorLogged)
                {
                    _logger.Log(BuddyId, "Specified path is not in Buddy directory: " + fullPath, BuddyLogLevel.Error);
                }

                _pathInvalidErrorLogged = true;
            }
            else if (loadResult is TextureLoadResult.FailureFileNotFound)
            {
                if (!_fileNotFoundErrorLogged)
                {
                    _logger.Log(BuddyId, "Specified file does not exist: " + fullPath, BuddyLogLevel.Error);
                }

                _fileNotFoundErrorLogged = true;
            }
        }
    }
}