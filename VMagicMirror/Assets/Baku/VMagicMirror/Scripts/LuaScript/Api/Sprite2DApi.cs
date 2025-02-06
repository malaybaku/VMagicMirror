using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Scripting;

namespace Baku.VMagicMirror.LuaScript.Api
{
    public class Sprite2DTransitionStyleValues
    {
        // NoneはScriptからは使えなくていいので公開してない
        //public Sprite2DTransitionStyle None => Sprite2DTransitionStyle.None;
        [Preserve]
        public int Immediate => (int) Sprite2DTransitionStyle.Immediate;
        [Preserve]
        public int LeftFlip => (int) Sprite2DTransitionStyle.LeftFlip;
        [Preserve]
        public int RightFlip => (int) Sprite2DTransitionStyle.RightFlip;
        
        private Sprite2DTransitionStyleValues() { }
        private static Sprite2DTransitionStyleValues _instance;
        public static Sprite2DTransitionStyleValues Instance => _instance ??= new Sprite2DTransitionStyleValues();
    }

    public enum Sprite2DTransitionStyle
    {
        None = 0,
        Immediate = 1,
        LeftFlip = 2,
        RightFlip = 3,
    }
    
    public class Sprite2DApi
    {
        // 毎フレームログ出力しないように…ということで
        private bool _fileNotFoundErrorLogged = false;
        private bool _pathInvalidErrorLogged = false;
        
        internal LuaScriptSpriteInstance Instance { get; set; }
        // NOTE: CurrentTransitionStyleがNone以外な場合、このテクスチャが実際に表示されているとは限らない
        internal Texture2D CurrentTexture { get; private set; }
        // NOTE: この値はトランジション処理をやっているクラスがトランジションを完了すると、自動でNoneに切り替わる
        internal Sprite2DTransitionStyle CurrentTransitionStyle { get; set; } = Sprite2DTransitionStyle.None;
        
        internal bool IsActive { get; private set; } = true;
        
        private readonly Dictionary<string, Texture2D> _textures = new();

        [Preserve]
        public Vector2 Position { get; set; }
        [Preserve]
        public Vector2 Size { get; set; }
        [Preserve]
        public SpriteEffectApi Effects { get; } = new();

        private readonly string _baseDir;

        internal Sprite2DApi(string baseDir)
        {
            _baseDir = baseDir;
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
        
        [Preserve]
        public void Hide() => IsActive = false;
        
        [Preserve]
        public void Show(string path) => Show(path, (int) Sprite2DTransitionStyle.Immediate);

        [Preserve]
        public void Show(string path, int style)
        {
            ApiUtils.Try(() =>
            {
                var fullPath = Path.Combine(_baseDir, path).ToLower();
                if (!Load(fullPath))
                {
                    return;
                }

                CurrentTexture = _textures[fullPath];
                var clampedStyle = Mathf.Clamp(
                    style, (int)Sprite2DTransitionStyle.None, (int)Sprite2DTransitionStyle.RightFlip
                    );
                CurrentTransitionStyle = (Sprite2DTransitionStyle)clampedStyle;
                IsActive = true;
            });
        }

        [Preserve]
        public void Preload(string path)
        {
            ApiUtils.Try(() =>
            {
                var fullPath = Path.Combine(_baseDir, path).ToLower();
                Load(fullPath);
            });
        }
        
        private bool Load(string fullPath)
        {
            if (_textures.ContainsKey(fullPath))
            {
                // cacheを使えばよい
                return true;
            }

            if (!ApiUtils.IsChildDirectory(SpecialFiles.BuddyRootDirectory, fullPath))
            {
                if (!_pathInvalidErrorLogged)
                {
                    LogOutput.Instance.Write("Specified path is not in SubCharacter directory: " + fullPath);
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
