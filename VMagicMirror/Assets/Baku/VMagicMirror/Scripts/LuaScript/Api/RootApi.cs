using System.Collections.Generic;
using UnityEngine;

namespace Baku.VMagicMirror.LuaScript.Api
{
    /// <summary>
    /// VMMのScriptでトップレベルから呼ぶ関数をここに入れる
    /// </summary>
    public class RootApi
    {
        private readonly List<SpriteApi> _sprites = new();
        public IReadOnlyList<SpriteApi> Sprites => _sprites; 
        
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

        // インスタンスの生成とかもこういう感じで。
        public SpriteApi CreateSprite()
        {
            var result = new SpriteApi();
            _sprites.Add(result);
            return result;
        }

        public Vector2 Vector2(float x, float y) => new(x, y);
    }
}
