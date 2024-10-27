using System;
using System.IO;
using System.Text;
using Baku.VMagicMirror.LuaScript.Api;
using NLua;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.LuaScript
{
    //TODO: Tickのタイミングだと困る可能性が結構高い(フレーム内のめっちゃ早い or 遅いタイミングにしたほうが良さそう)
    public class ScriptCaller : PresenterBase, ITickable
    {
        // リロードに対応するとか色々なアレがあります、多分…
        private RootApi _api;
        private Lua _lua;

        //「定義されてれば呼ぶ」系のコールバックメソッド。今のとこupdateしかないが他も増やしてよい
        private LuaFunction _updateFunction;
        

        private readonly LuaScriptSpriteCanvas _spriteCanvas;
        
        [Inject]
        public ScriptCaller(LuaScriptSpriteCanvas spriteCanvas)
        {
            _spriteCanvas = spriteCanvas;
        }
        
        public override void Initialize()
        {
            _api = new RootApi();
            _lua = new Lua();
            _lua.State.Encoding = Encoding.UTF8;
            _lua["api"] = _api;
            
            var entryScriptPath = Path.Combine(SpecialFiles.ScriptDirectory, "main.lua");
            if (!File.Exists(entryScriptPath))
            {
                Debug.Log("script does not exists.");
                return;
            }

            try
            {
                _lua.DoString(File.ReadAllText(entryScriptPath));
                _updateFunction = _lua["update"] as LuaFunction;
                
                if (_lua["start"] is LuaFunction function)
                {
                    function.Call();
                }
            }
            catch (Exception e)
            {
                LogOutput.Instance.Write("Failed to load script:");
                LogOutput.Instance.Write(e);
                Debug.LogException(e);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _lua?.Dispose();
            _lua = null;
        }

        void ITickable.Tick()
        {
            try
            {
                _updateFunction?.Call(Time.deltaTime);
            }
            catch (Exception e)
            {
                LogOutput.Instance.Write("Error in lua script update:");
                LogOutput.Instance.Write(e);
                Debug.LogException(e);
            }
            
            foreach (var sprite in _api.Sprites)
            {
                UpdateSprite(sprite);
            }
        }

        private void UpdateSprite(SpriteApi sprite)
        {
            if (sprite.Instance == null)
            {
                sprite.Instance = _spriteCanvas.CreateInstance();
            }
            
            // 色々と毎フレーム呼ばないで済む方が嬉しい…
            sprite.Instance.SetPosition(sprite.Position);
            sprite.Instance.SetSize(sprite.Size);
            sprite.Instance.SetTexture(sprite.CurrentTexture);
        }
    }
}
