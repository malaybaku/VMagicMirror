using UnityEngine;

namespace Baku.VMagicMirror.Buddy.Api
{
    public class ScreenApi
    {
        public int Width() => Screen.width;
        public int Height() => Screen.height;

        //TODO: 透過中かどうかを返したい
        public bool IsTransparent => false;
    }
}
