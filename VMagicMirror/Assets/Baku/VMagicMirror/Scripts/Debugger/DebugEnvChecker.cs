using UnityEngine;

namespace Baku.VMagicMirror
{
    public static class DebugEnvChecker
    {
        public static bool IsDevEnv => 
#if DEV_ENV
            true;
#else
            false;
#endif

        public static bool IsDevEnvOrEditor => IsDevEnv || Application.isEditor;
    }
}
