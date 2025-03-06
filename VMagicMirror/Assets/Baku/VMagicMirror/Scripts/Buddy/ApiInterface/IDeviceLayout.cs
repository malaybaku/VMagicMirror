namespace Baku.VMagicMirror.Buddy.Api.Interface
{
    public interface IDeviceLayout
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
