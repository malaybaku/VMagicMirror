using Baku.VMagicMirror.Buddy.Api.Interface;

namespace Baku.VMagicMirror.Buddy.Api
{
    public class DeviceLayoutApi : IDeviceLayout
    {
        private readonly DeviceLayoutApiImplement _impl;
        public DeviceLayoutApi(DeviceLayoutApiImplement impl)
        {
            _impl = impl;
        }

        public Interface.Pose GetCameraPose() => _impl.GetCameraPose().ToApiValue();
        public float GetCameraFov() => _impl.GetCameraFov();

        public Interface.Pose GetKeyboardPose() => _impl.GetKeyboardPose().ToApiValue();
        public Interface.Pose GetTouchpadPose() => _impl.GetTouchpadPose().ToApiValue();
        public Interface.Pose GetPenTabletPose() => _impl.GetPenTabletPose().ToApiValue();
        public Interface.Pose GetGamepadPose() => _impl.GetGamepadPose().ToApiValue();
        public bool GetKeyboardVisible() => _impl.GetKeyboardVisible();
        public bool GetTouchpadVisible() => _impl.GetTouchpadVisible();
        public bool GetPenTabletVisible() => _impl.GetPenTabletVisible();
        public bool GetGamepadVisible() => _impl.GetGamepadVisible();
    }
}
