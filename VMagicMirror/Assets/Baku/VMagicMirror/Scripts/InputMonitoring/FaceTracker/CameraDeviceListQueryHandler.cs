using System.Linq;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public sealed class CameraDeviceListQueryHandler : PresenterBase
    {
        private readonly IMessageReceiver _receiver;

        public CameraDeviceListQueryHandler(IMessageReceiver receiver)
        {
            _receiver = receiver;
        }
        
        public override void Initialize()
        {
            _receiver.AssignQueryHandler(
                VmmCommands.CameraDeviceNames,
                query => query.Result = DeviceNames.CreateDeviceNamesJson(GetCameraDeviceNames())
                );
        }

        private static string[] GetCameraDeviceNames() => WebCamTexture.devices
            .Select(d => d.name)
            .ToArray();
    }
}