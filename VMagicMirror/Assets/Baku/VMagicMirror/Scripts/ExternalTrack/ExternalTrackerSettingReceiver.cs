using UnityEngine;
using Baku.VMagicMirror.ExternalTracker;
using Baku.VMagicMirror.InterProcess;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 外部デバイスによる顔トラの設定を受け取るレシーバクラスです。
    /// </summary>
    public class ExternalTrackerSettingReceiver : MonoBehaviour
    {
        //TODO: 非MonoBehaviour化できそう
        [Inject]
        public void Initialize(IMessageReceiver receiver, ExternalTrackerDataSource dataSource)
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
            
            //NOTE: 以下は今のところハンドリングしたい内容が無いため無視
            //MessageCommandNames.ExTrackerSetApplicationValue:
        }
    }
}
