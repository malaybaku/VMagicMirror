using UnityEngine;
using Zenject;
using UniRx;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// <see cref="FaceControlConfiguration"/>のうち、制御モードについてプロセス間通信をもとに書き込むクラスです。
    /// </summary>
    public class FaceControlConfigurationReceiver : MonoBehaviour
    {
        private bool _enableWebCamTracking = true;
        private bool _enableExTracker = false;

        [Inject]
        public void Initialize(ReceivedMessageHandler handler, FaceControlConfiguration config)
        {
            handler.Commands.Subscribe(message =>
            {
                switch (message.Command)
                {
                    case MessageCommandNames.EnableFaceTracking:
                        _enableWebCamTracking = message.ToBoolean();
                        SetFaceControlMode();
                        break;
                    case MessageCommandNames.ExTrackerEnable:
                        _enableExTracker = message.ToBoolean();
                        SetFaceControlMode();
                        break;
                }
            });
            
            void SetFaceControlMode()
            {
                config.ControlMode =
                    _enableExTracker ? FaceControlModes.ExternalTracker :
                    _enableWebCamTracking ? FaceControlModes.WebCam :
                    FaceControlModes.None;
            }
        }
    }
}
