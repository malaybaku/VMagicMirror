using BuddyApi = VMagicMirror.Buddy;

namespace Baku.VMagicMirror.Buddy.Api
{
    public class DeviceLayoutApi : BuddyApi.IDeviceLayout
    {
        private readonly DeviceLayoutApiImplement _impl;
        public DeviceLayoutApi(DeviceLayoutApiImplement impl)
        {
            _impl = impl;
        }

        public override string ToString() => nameof(BuddyApi.IDeviceLayout);

        public BuddyApi.Pose GetCameraPose() => _impl.GetCameraPose().ToApiValue();
        public float GetCameraFov() => _impl.GetCameraFov();

        public BuddyApi.Pose GetKeyboardPose() => _impl.GetKeyboardPose().ToApiValue();
        public BuddyApi.Pose GetTouchpadPose() => _impl.GetTouchpadPose().ToApiValue();
        public BuddyApi.Pose GetPenTabletPose() => _impl.GetPenTabletPose().ToApiValue();
        public BuddyApi.Pose GetGamepadPose() => _impl.GetGamepadPose().ToApiValue();
        public bool GetKeyboardVisible() => _impl.GetKeyboardVisible();
        public bool GetTouchpadVisible() => _impl.GetTouchpadVisible();
        public bool GetPenTabletVisible() => _impl.GetPenTabletVisible();
        public bool GetGamepadVisible() => _impl.GetGamepadVisible();
    }
}
