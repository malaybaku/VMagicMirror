using VMagicMirror.Buddy;
using UnityEngine;

namespace Baku.VMagicMirror.Buddy.Api
{
    public class ScreenApi : IScreen
    {
        private readonly ScreenApiImplement _impl;
        
        public ScreenApi(ScreenApiImplement impl)
        {
            _impl = impl;
        }

        public int Width => Screen.width;
        public int Height => Screen.height;
        public bool IsTransparent => _impl.IsTransparent;
    }
}
