using UnityEngine;

namespace Baku.VMagicMirror.LuaScript.Api
{
    public class ScreenApi
    {
        public int Width() => Screen.width;
        public int Height() => Screen.height;

        //TODO: 透過中かどうかを返したい
        public bool IsTransparent => false;
    }
}
