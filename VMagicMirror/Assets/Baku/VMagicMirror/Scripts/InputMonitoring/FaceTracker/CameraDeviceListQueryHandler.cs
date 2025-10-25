using System.Linq;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public sealed class CameraDeviceListQueryHandler
    {
        public CameraDeviceListQueryHandler(IMessageReceiver receiver)
        {
            receiver.AssignQueryHandler(
                VmmCommands.CameraDeviceNames,
                query =>
                {
                    Debug.Log("Return camera device names...");
                    query.Result = DeviceNames.CreateDeviceNamesJson(GetCameraDeviceNames());
                });
        }

        private static string[] GetCameraDeviceNames() => WebCamTexture.devices
            .Select(d => d.name)
            .ToArray();
    }
}