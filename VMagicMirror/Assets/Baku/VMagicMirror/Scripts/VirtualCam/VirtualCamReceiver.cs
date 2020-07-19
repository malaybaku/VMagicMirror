namespace Baku.VMagicMirror
{
    public class VirtualCamReceiver
    {
        public VirtualCamReceiver(IMessageReceiver receiver, VirtualCamCapture capture)
        {
            receiver.AssignCommandHandler(
                VmmCommands.SetVirtualCamEnable,
                c => capture.EnableCapture = c.ToBoolean()
                );
            receiver.AssignCommandHandler(
                VmmCommands.SetVirtualCamWidth,
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
                VmmCommands.SetVirtualCamHeight,
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