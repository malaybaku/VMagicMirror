using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Scripting;

namespace Baku.VMagicMirror.LuaScript.Api
{
    /// <summary>
    /// VMMのScriptでトップレベルから呼ぶ関数をここに入れる
    /// </summary>
    [Preserve]
    public class RootApi
    {
        private readonly List<Sprite2DApi> _sprites = new();
        public IReadOnlyList<Sprite2DApi> Sprites => _sprites;

        private readonly string _baseDir;
        public RootApi(string baseDir)
        {
            _baseDir = baseDir;
        }

        internal void Dispose()
        {
            foreach (var sprite in _sprites)
            {
                sprite.Dispose();
            }
            _sprites.Clear();
        }

        //NOTE: スクリプトが呼ばれる前に非nullで初期化されるのが期待値
        [Preserve]
        public PropertyApi Property { get; internal set; } = null;

        [Preserve]
        public void Log(string value)
        {
            if (Application.isEditor)
            {
                Debug.Log(value);
            }
            else
            {
                LogOutput.Instance.Write(value);
            }
        }

        [Preserve]
        public bool ValidateFilePath(string path)
        {
            var fullPath = Path.Combine(_baseDir, path);
            return
                ApiUtils.IsChildDirectory(SpecialFiles.BuddyRootDirectory, fullPath) &&
                File.Exists(path);
        }
        
        [Preserve]
        public Sprite2DApi Create2DSprite()
        {
            var result = new Sprite2DApi(_baseDir);
            _sprites.Add(result);
            return result;
        }
        
        [Preserve]
        public Vector2 Vector2(float x, float y) => new(x, y);
    }
}
