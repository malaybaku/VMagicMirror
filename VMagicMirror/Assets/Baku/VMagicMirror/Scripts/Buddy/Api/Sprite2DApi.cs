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

    public class Sprite2DApi : ISprite2DApi
    {
        private readonly string _baseDir;
        private readonly string _buddyId;
        private bool _fileNotFoundErrorLogged;
        private bool _pathInvalidErrorLogged;
        
        internal Sprite2DApi(string baseDir, string buddyId)
        {
            _baseDir = baseDir;
            _buddyId = buddyId;
        }

        // TODO: Instanceを見たいときにAPI経由で参照しないで済むようにしたい
        internal BuddySpriteInstance Instance { get; set; }
        internal void Dispose() => Instance.Dispose();

        Vector2 ISprite2DApi.LocalPosition
        {
            get => Instance.LocalPosition.ToApiValue();
            set => Instance.LocalPosition = value.ToEngineValue();
        }

        Quaternion ISprite2DApi.LocalRotation
        {
            get => Instance.LocalRotation.ToApiValue();
            set => Instance.LocalRotation = value.ToEngineValue();
        }

        Vector2 ISprite2DApi.Size
        {
            get => Instance.Size.ToApiValue();
            set => Instance.Size = value.ToEngineValue();
        }
        
        Vector2 ISprite2DApi.Pivot
        {
            get => Instance.Pivot.ToApiValue();
            set => Instance.Pivot = value.ToEngineValue();
        }

        ISpriteEffectApi ISprite2DApi.Effects => Instance.SpriteEffects;

        public void Preload(string path) => ApiUtils.Try(_buddyId, () =>
        {
            var fullPath = GetFullPath(path);
            var result = Instance.Load(fullPath);
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

                var loadResult = Instance.Show(fullPath, (Sprite2DTransitionStyle)clamped);
                HandleTextureLoadResult(fullPath, loadResult);
                if (loadResult == TextureLoadResult.Success)
                {
                    Instance.SetActive(true);
                }
            });
        }

        public void Hide() => Instance.SetActive(false);
        
        public void SetPosition(Vector2 position) => Instance.SetPosition(position.ToEngineValue());

        public void SetParent(ITransform2DApi parent)
        {
            var parentInstance = ((Transform2DApi)parent).GetInstance();
            Instance.SetParent(parentInstance);
        }

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