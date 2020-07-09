namespace Baku.VMagicMirror
{
    public class VirtualCamReceiver
    {
        public VirtualCamReceiver(IMessageReceiver receiver, VirtualCamCapture capture)
        {
            receiver.AssignCommandHandler(
                MessageCommandNames.SetVirtualCamEnable,
                c => capture.EnableCaptureWrite = c.ToBoolean()
                );
            receiver.AssignCommandHandler(
                MessageCommandNames.SetVirtualCamWidth,
                c =>
                {
                    //NOTE: 4の倍数だけ通すのはストライドとかそういうアレです
                    int width = c.ToInt();
                    if (width >= 80 && width <= 1920 && width % 4 == 0)
                    {
                        capture.Width = width;
                    }
                });
            receiver.AssignCommandHandler(
                MessageCommandNames.SetVirtualCamHeight,
                c => {
                    int height = c.ToInt();
                    if (height >= 80 && height < 1920 && height % 4 == 0)
                    {
                        capture.Height = height;
                    }
                });            
        }
    }
}