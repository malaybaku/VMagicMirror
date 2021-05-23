using System;
using UnityEngine;

namespace Baku.VMagicMirror
{
    [Serializable]
    public class DeviceNames
    {
        public string[] Names;

        public static string CreateDeviceNamesJson(string[] names)
        {
            return JsonUtility.ToJson(new DeviceNames()
            {
                Names = names,
            });
        }
    }
}
