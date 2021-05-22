using Baku.VMagicMirror.ExternalTracker;

namespace Baku.VMagicMirror
{
    /// <summary> 外部デバイスによる顔トラの設定を受け取るレシーバー </summary>
    public class ExternalTrackerSettingReceiver
    {
        public ExternalTrackerSettingReceiver(IMessageReceiver receiver, ExternalTrackerDataSource dataSource)
        {
            receiver.AssignCommandHandler(
                VmmCommands.ExTrackerEnable,
                c => dataSource.EnableTracking(c.ToBoolean())
                );
            receiver.AssignCommandHandler(
                VmmCommands.ExTrackerCalibrate,
                _ => dataSource.Calibrate()
                );
            receiver.AssignCommandHandler(
                VmmCommands.ExTrackerSetCalibrateData,
                c => dataSource.SetCalibrationData(c.Content)
                );
            receiver.AssignCommandHandler(
                VmmCommands.ExTrackerSetSource,
                c => dataSource.SetSourceType(c.ToInt())
                );
            receiver.AssignCommandHandler(
                VmmCommands.ExTrackerSetFaceSwitchSetting,
                c => dataSource.SetFaceSwitchSetting(c.Content)
                );
            
            receiver.AssignCommandHandler(
                VmmCommands.DisableFaceTrackingHorizontalFlip,
                c => dataSource.SetFaceHorizontalFlipDisable(c.ToBoolean())
                );
        }
    }
}
