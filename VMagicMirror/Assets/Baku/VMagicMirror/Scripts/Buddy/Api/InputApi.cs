using NLua;
using UnityEngine;
using UnityEngine.Scripting;

namespace Baku.VMagicMirror.Buddy.Api
{
    public class InputApi
    {
        private readonly InputApiImplement _impl;

        public InputApi(InputApiImplement impl)
        {
            _impl = impl;
        }
     
        [Preserve] public Vector2 MousePosition => _impl.GetNonDimensionalMousePosition();

        // GamePadButton(int)の引数が欲しい
        [Preserve] public LuaFunction GamepadButtonDown { get; set; }
        [Preserve] public LuaFunction GamepadButtonUp { get; set; }

        // ENTER以外のキー名は通知されない (全て "" になる) 
        [Preserve] public LuaFunction KeyboardKeyDown { get; set; }
        [Preserve] public LuaFunction KeyboardKeyUp { get; set; }
        
        // TODO: enumの変換を噛ませたい
        [Preserve] public bool GetGamepadButton(GamepadKey key) => _impl.GetGamepadButton(key);

        
    }
}

