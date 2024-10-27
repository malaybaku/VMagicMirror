using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Baku.VMagicMirror.LuaScript.Api
{
    public class SpriteApi
    {
        internal LuaScriptSpriteInstance Instance { get; set; }
        internal Texture2D CurrentTexture { get; private set; }
        internal bool IsActive { get; private set; } = true;
        
        private readonly Dictionary<string, Texture2D> _textures = new();

        public Vector2 Position { get; set; }
        public Vector2 Size { get; set; }
        
        public void Load(string path)
        {
            //NOTE: Scriptsフォルダから上に行くのを禁止したい
            var fullPath = Path.Combine(SpecialFiles.ScriptDirectory, path);
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning("file not found: " + fullPath);
                return;
            }

            var bytes = File.ReadAllBytes(fullPath);
            var texture = new Texture2D(128, 128);
            texture.LoadImage(bytes);
            texture.Apply(false, true);

            //pathの字面が違っても同じファイルを指すことがあるが、深く考えずに別物と考えてしまう
            _textures[path] = texture;
            if (_textures.Count == 1)
            {
                CurrentTexture = texture;
            }
        }

        public void Activate(string path)
        {
            if (_textures.TryGetValue(path, out var texture))
            {
                CurrentTexture = texture;
            }
        }

        public void Show() => IsActive = true;
        public void Hide() => IsActive = false;
    }
}
