using UnityEngine;

namespace Baku.VMagicMirror.Buddy.Api
{
    public class DeviceLayoutApi
    {
        private readonly DeviceLayoutApiImplement _impl;
        public DeviceLayoutApi(DeviceLayoutApiImplement impl)
        {
            _impl = impl;
        }

        public Pose GetCameraPose() => _impl.GetCameraPose();
        public float GetCameraFov() => _impl.GetCameraFov();
        public Pose GetKeyboardPose() => _impl.GetKeyboardPose();
        public Pose GetTouchpadPose() => _impl.GetTouchpadPose();
        public Pose GetPenTabletPose() => _impl.GetPenTabletPose();
        public Pose GetGamepadPose() => _impl.GetGamepadPose();
        public bool GetKeyboardVisible() => _impl.GetKeyboardVisible();
        public bool GetTouchpadVisible() => _impl.GetTouchpadVisible();
        public bool GetPenTabletVisible() => _impl.GetPenTabletVisible();
        public bool GetGamepadVisible() => _impl.GetGamepadVisible();
    }
}
