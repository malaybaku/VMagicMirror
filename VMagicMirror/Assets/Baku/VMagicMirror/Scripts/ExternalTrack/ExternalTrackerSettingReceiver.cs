using Baku.VMagicMirror.ExternalTracker;

namespace Baku.VMagicMirror
{
    /// <summary> 外部デバイスによる顔トラの設定を受け取るレシーバー </summary>
    public class ExternalTrackerSettingReceiver
    {
        public ExternalTrackerSettingReceiver(IMessageReceiver receiver, ExternalTrackerDataSource dataSource)
        {
            receiver.AssignCommandHandler(
                MessageCommandNames.ExTrackerEnable,
                c => dataSource.EnableTracking(c.ToBoolean())
                );
            receiver.AssignCommandHandler(
                MessageCommandNames.ExTrackerCalibrate,
                _ => dataSource.Calibrate()
                );
            receiver.AssignCommandHandler(
                MessageCommandNames.ExTrackerSetCalibrateData,
                c => dataSource.SetCalibrationData(c.Content)
                );
            receiver.AssignCommandHandler(
                MessageCommandNames.ExTrackerSetSource,
                c => dataSource.SetSourceType(c.ToInt())
                );
            receiver.AssignCommandHandler(
                MessageCommandNames.ExTrackerSetFaceSwitchSetting,
                c => dataSource.SetFaceSwitchSetting(c.Content)
                );
        }
    }
}
