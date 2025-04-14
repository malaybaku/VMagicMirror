using System.IO;
using UnityEngine;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    public static class FilePathUtil
    {
        public static string GetModelFilePath(string fileName)
        {
            return Path.Combine(
                Application.streamingAssetsPath,
                "MediapipeTracker",
                fileName
            );
        }
    }
}
