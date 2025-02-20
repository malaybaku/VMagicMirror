namespace Baku.VMagicMirror.Buddy.Api.Interface
{
    public interface IDeviceLayoutApi
    {
        Pose GetCameraPose();
        float GetCameraFov();
        Pose GetKeyboardPose();
        Pose GetTouchpadPose();
        Pose GetPenTabletPose();
        Pose GetGamepadPose();
        bool GetKeyboardVisible();
        bool GetTouchpadVisible();
        bool GetPenTabletVisible();
        bool GetGamepadVisible();
    }
}
