using UnityEngine;
using Zenject;
using Baku.VMagicMirror.InterProcess;

namespace Baku.VMagicMirror
{
    [RequireComponent(typeof(VirtualCamCapture))]
    public class VirtualCamReceiver : MonoBehaviour
    {
        private VirtualCamCapture _capture;
        
        //TODO: 非MonoBehaviour化
        [Inject]
        public void Initialize(IMessageReceiver receiver)
        {
            receiver.AssignCommandHandler(
                MessageCommandNames.SetVirtualCamEnable,
                c => _capture.EnableCaptureWrite = c.ToBoolean()
                );
            receiver.AssignCommandHandler(
                MessageCommandNames.SetVirtualCamWidth,
                c =>
                {
                    //NOTE: 4の倍数だけ通すのはストライドとかそういうアレです
                    int width = c.ToInt();
                    if (width >= 80 && width <= 1920 && width % 4 == 0)
                    {
                        _capture.Width = width;
                    }
                });
            receiver.AssignCommandHandler(
                MessageCommandNames.SetVirtualCamHeight,
                c => {
                    int height = c.ToInt();
                    if (height >= 80 && height < 1920 && height % 4 == 0)
                    {
                        _capture.Height = height;
                    }
                });            
        }

        private void Start()
        {
            _capture = GetComponent<VirtualCamCapture>();
        }
    }
}