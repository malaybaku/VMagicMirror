using UnityEngine;
using Zenject;
using Baku.VMagicMirror.InterProcess;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// <see cref="FaceControlConfiguration"/>のうち、制御モードについてプロセス間通信をもとに書き込むクラスです。
    /// </summary>
    public class FaceControlConfigurationReceiver : MonoBehaviour
    {
        private bool _enableWebCamTracking = true;
        private bool _enableExTracker = false;

        //TODO: 非MonoBehaviour化できそう
        [Inject]
        public void Initialize(IMessageReceiver receiver, FaceControlConfiguration config)
        {
            receiver.AssignCommandHandler(
                MessageCommandNames.EnableFaceTracking,
                message =>
                {
                    _enableWebCamTracking = message.ToBoolean();
                    SetFaceControlMode();
                });
            receiver.AssignCommandHandler(
                MessageCommandNames.ExTrackerEnable,
                message =>
                {
                    _enableExTracker = message.ToBoolean();
                    SetFaceControlMode();
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
