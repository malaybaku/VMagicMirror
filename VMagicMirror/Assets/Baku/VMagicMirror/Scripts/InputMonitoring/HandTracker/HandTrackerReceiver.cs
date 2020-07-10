namespace Baku.VMagicMirror
{
    /// <summary>
    /// 画像ベースのハンドトラッキング処理まわりのレシーバークラス
    /// </summary>
    public class HandTrackerReceiver
    {
        public HandTrackerReceiver(IMessageReceiver receiver, HandTracker handTracker)
        {
            receiver.AssignCommandHandler(
                VmmCommands.EnableImageBasedHandTracking,
                c => handTracker.ImageProcessEnabled = c.ToBoolean()
            );
        }
    }
}
