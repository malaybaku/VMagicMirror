using Unity.Collections;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public class MemoryLeakDetectionSetup : MonoBehaviour
    {
        private void Start()
        {
            NativeLeakDetection.Mode = NativeLeakDetectionMode.EnabledWithStackTrace;
        }
    }
}
