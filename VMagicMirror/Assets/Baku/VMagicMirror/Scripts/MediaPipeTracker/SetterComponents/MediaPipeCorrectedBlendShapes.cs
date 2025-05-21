using UnityEngine;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    public class MediaPipeCorrectedBlendShapes : IFaceTrackBlendShapes
    {
        public MediaPipeCorrectedBlendShapes(
            IFaceTrackBlendShapes src,
            MediaPipeTrackerRuntimeSettingsRepository settings)
        {
            _src = src;
            _settings = settings;
        }
        
        private readonly IFaceTrackBlendShapes _src;
        private readonly MediaPipeTrackerRuntimeSettingsRepository _settings;

        // srcの更新時に呼ぶことで、補正込みの目のBlendShape値を取得する
        public void UpdateEye()
        {
            var srcEye = _src.Eye;
            _eye.LeftLookUp = srcEye.LeftLookUp;
            _eye.LeftLookDown = srcEye.LeftLookDown;
            _eye.LeftLookIn = srcEye.LeftLookIn;
            _eye.LeftLookOut = srcEye.LeftLookOut;

            _eye.RightLookUp = srcEye.RightLookUp;
            _eye.RightLookDown = srcEye.RightLookDown;
            _eye.RightLookIn = srcEye.RightLookIn;
            _eye.RightLookOut = srcEye.RightLookOut;

            // wideは補正したほうがいい可能性もあるが、見た感じではMediaPipeが有効な値をほぼ喋ってなさそうなので素通しする
            _eye.LeftWide = srcEye.LeftWide;
            _eye.RightWide = srcEye.RightWide;
            
            // ストレートに適用できるケース: そのまま適用しておしまい
            if (!_settings.EyeApplyCorrectionToPerfectSync.Value)
            {
                _eye.LeftBlink = srcEye.LeftBlink;
                _eye.LeftSquint = srcEye.LeftSquint;
                _eye.RightBlink = srcEye.RightBlink;
                _eye.RightSquint = srcEye.RightSquint;
                return;
            }

            // やること
            // - blinkの値が0, 1いずれかで極端になるように補正する。
            // - blinkの値が0か1付近になるとsquintを0寄りにする(…でいいよね…?)
            var leftBlink = MapClamp(srcEye.LeftBlink, _settings.EyeOpenBlinkValue, _settings.EyeCloseBlinkValue);
            var leftSquint = SquintWeight(leftBlink) * srcEye.LeftSquint;

            var rightBlink = MapClamp(srcEye.RightBlink, _settings.EyeOpenBlinkValue, _settings.EyeCloseBlinkValue);
            var rightSquint = SquintWeight(rightBlink) * srcEye.RightSquint;

            if (_settings.EyeUseMeanBlinkValue.Value)
            {
                var blink = (leftBlink + rightBlink) / 2;
                var squint = (leftSquint + rightSquint) / 2;
                _eye.LeftBlink = blink;
                _eye.LeftSquint = squint;
                _eye.RightBlink = blink;
                _eye.RightSquint = squint;
            }
            else
            {
                _eye.LeftBlink = leftBlink;
                _eye.LeftSquint = leftSquint;
                _eye.RightBlink = rightBlink;
                _eye.RightSquint = rightSquint;
            }
        }

        private readonly EyeBlendShape _eye = new();
        public EyeBlendShape Eye => _eye;
        public BrowBlendShape Brow => _src.Brow;
        public MouthBlendShape Mouth => _src.Mouth;
        public JawBlendShape Jaw => _src.Jaw;
        public CheekBlendShape Cheek => _src.Cheek;
        public NoseBlendShape Nose => _src.Nose;
        public TongueBlendShape Tongue => _src.Tongue;
        
        //0-1の範囲の値をmin-maxの幅のなかにギュッとあれします
        private static float MapClamp(float value, float min, float max)
        {
            if (value < min)
            {
                return 0f;
            }
            else if (value > max)
            {
                return 1f;
            }
            else
            {
                return Mathf.Clamp01((value - min) / (max - min));
            }
        }

        // f(0) = f(1) = 0, f(0.5) = 1 であるような二次曲線
        private static float SquintWeight(float value) 
            => 4 * value * (1 - value);
    }
}
