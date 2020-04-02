using UnityEngine;
using Zenject;
using UniRx;

namespace Baku.VMagicMirror
{
    [RequireComponent(typeof(VirtualCamCapture))]
    public class VirtualCamReceiver : MonoBehaviour
    {
        [Inject] private ReceivedMessageHandler _handler = null;

        private void Start()
        {
            var capture = GetComponent<VirtualCamCapture>();
            
            _handler.Commands.Subscribe(c =>
            {
                switch (c.Command)
                {
                    case MessageCommandNames.SetVirtualCamEnable:
                        capture.EnableCaptureWrite = c.ToBoolean();
                        break;
                    case MessageCommandNames.SetVirtualCamWidth:
                        //NOTE: 4の倍数だけ通すのはストライドとかそういうアレです
                        int width = c.ToInt();
                        if (width >= 80 && width % 4 == 0)
                        {
                            capture.Width = width;
                        }
                        break;
                    case MessageCommandNames.SetVirtualCamHeight:
                        int height = c.ToInt();
                        if (height >= 80 && height % 4 == 0)
                        {
                            capture.Height = height;
                        }
                        break;
                }
            });
        }
    }
}