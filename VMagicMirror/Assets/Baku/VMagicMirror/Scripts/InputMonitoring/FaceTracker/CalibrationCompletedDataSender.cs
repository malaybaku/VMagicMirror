namespace Baku.VMagicMirror
{
    /// <summary> キャリブレーションの完了時処理を定義します。 </summary>
    public class CalibrationCompletedDataSender
    {
        public CalibrationCompletedDataSender(IMessageSender sender, FaceTracker faceTracker)
        {
            faceTracker.CalibrationCompleted += 
                data => sender.SendCommand(MessageFactory.Instance.SetCalibrationFaceData(data));
        }
    }
}

