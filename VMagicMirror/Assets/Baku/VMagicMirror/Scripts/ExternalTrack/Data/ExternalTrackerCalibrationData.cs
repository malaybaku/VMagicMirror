using System;

namespace Baku.VMagicMirror.ExternalTracker
{
    /// <summary>
    /// トラッキングアプリ名をキーとし、各アプリのキャリブレーション情報が文字列として入るような、キャリブレーション情報です。
    /// </summary>
    [Serializable]
    public class ExternalTrackerCalibrationData
    {
        public string iFacialMocap;
        public string waidayo;
    }
}
