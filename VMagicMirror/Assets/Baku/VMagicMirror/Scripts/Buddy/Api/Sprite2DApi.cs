using System.IO;
using Baku.VMagicMirror.Buddy.Api.Interface;

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
        private readonly string _buddyId;
        private readonly BuddySprite2DInstance _instance;

        private bool _fileNotFoundErrorLogged;
        private bool _pathInvalidErrorLogged;

        internal Sprite2DApi(string baseDir, string buddyId, BuddySprite2DInstance instance)
        {
            _baseDir = baseDir;
            _buddyId = buddyId;
            _instance = instance;
            Transform = new Transform2D(instance.GetTransform2DInstance());
        }

        public ITransform2D Transform { get; }

        Vector2 ISprite2D.Size
        {
            get => _instance.Size.ToApiValue();
            set => _instance.Size = value.ToEngineValue();
        }

        ISpriteEffect ISprite2D.Effects => _instance.SpriteEffects;

        public void Preload(string path) => ApiUtils.Try(_buddyId, () =>
        {
            var fullPath = GetFullPath(path);
            var result = _instance.Load(fullPath);
            HandleTextureLoadResult(fullPath, result);
        });

        public void Show(string path) => Show(path, Interface.Sprite2DTransitionStyle.Immediate);

        public void Show(string path, Interface.Sprite2DTransitionStyle style)
        {
            ApiUtils.Try(_buddyId, () =>
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
                    BuddyLogger.Instance.Log(_buddyId, "[Error] Specified path is not in Buddy directory: " + fullPath);
                }

                _pathInvalidErrorLogged = true;
            }
            else if (loadResult is TextureLoadResult.FailureFileNotFound)
            {
                if (!_fileNotFoundErrorLogged)
                {
                    BuddyLogger.Instance.Log(_buddyId, "[Error] Specified file does not exist: " + fullPath);
                }

                _fileNotFoundErrorLogged = true;
            }
        }
    }
}