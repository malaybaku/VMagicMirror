using UnityEngine;
using Baku.VMagicMirror.ExternalTracker;
using Zenject;
using UniRx;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 外部デバイスによる顔トラの設定を受け取るレシーバクラスです。
    /// </summary>
    public class ExternalTrackerSettingReceiver : MonoBehaviour
    {
        [Inject]
        public void Initialize(ReceivedMessageHandler handler) => _handler = handler;
        private ReceivedMessageHandler _handler;

        [SerializeField] private ExternalTrackerDataSource dataSource = null;
        
        private void Start()
        {
            _handler.Commands.Subscribe(c =>
            {
                switch (c.Command)
                {
                    case MessageCommandNames.ExTrackerEnable:
                        dataSource.EnableTracking(c.ToBoolean());
                        break;
                    case MessageCommandNames.ExTrackerCalibrate:
                        dataSource.Calibrate();
                        break;
                    case MessageCommandNames.ExTrackerSetCalibrateData:
                        dataSource.SetCalibrationData(c.Content);
                        break;
                    case MessageCommandNames.ExTrackerSetSource:
                        dataSource.SetSourceType(c.ToInt());
                        break;
                    case MessageCommandNames.ExTrackerSetFaceSwitchSetting:
                        dataSource.SetFaceSwitchSetting(c.Content);
                        break;
                    case MessageCommandNames.ExTrackerSetApplicationValue:
                        //NOTE: アプリ別の設定を受けられる、が今のところ必要なデータが無いので無視。
                        //ポート番号とか可変にしたいかって話なんだけど、そんなに嬉しくないんですよねえ…
                        break;
                    default:
                        break;
                }
            });
        }
    }
}
